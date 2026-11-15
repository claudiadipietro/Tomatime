using Microsoft.EntityFrameworkCore;
using Tomatime.Api.Context;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class SessionRunService(AppDbContext db) : ISessionRunService
{
    private static readonly TimeSpan ActivityTimeout = TimeSpan.FromMinutes(2);

    public async Task<ActiveSession?> HeartbeatAsync(int userId, int sessionId)
    {
        var now = DateTime.UtcNow;
        var updated = await db.Sessions
            .Where(s => s.Id == sessionId && s.UserId == userId &&
                        s.Status == SessionStatus.InProgress && s.LastActivityAt >= now - ActivityTimeout)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastActivityAt, now));
        if (updated == 0)
        {
            await CancelExpiredAsync();
            return null;
        }

        return await db.Sessions.AsNoTracking()
            .Where(s => s.Id == sessionId && s.UserId == userId && s.Status == SessionStatus.InProgress)
            .Select(s => new ActiveSession(s.Id, s.Duration, s.ActualStartTime!.Value, now))
            .SingleOrDefaultAsync();
    }

    public async Task<bool> CancelAsync(int userId, int sessionId) =>
        await db.Sessions.Where(s => s.Id == sessionId && s.UserId == userId &&
                                     s.Status == SessionStatus.InProgress)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, SessionStatus.Cancelled)
                .SetProperty(x => x.EndedAt, DateTime.UtcNow)) != 0;

    public async Task<CompletionResult> CompleteAsync(int userId, int sessionId)
    {
        var now = DateTime.UtcNow;
        var session = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(s =>
            s.Id == sessionId && s.UserId == userId);
        if (session?.Status == SessionStatus.Completed)
            return new(CompletionStatus.Completed, new SessionCompletion(session.PomosEarned));
        if (session?.Status != SessionStatus.InProgress)
            return new(CompletionStatus.NotFound);
        if (!SessionDuration.IsValid(session.Duration))
        {
            await CancelAsync(userId, sessionId);
            return new(CompletionStatus.NotFound);
        }
        if (session?.ActualStartTime is null || session.LastActivityAt is null ||
            session.LastActivityAt < now - ActivityTimeout)
        {
            await CancelExpiredAsync();
            return new(CompletionStatus.NotFound);
        }

        var totalMinutes = SessionTiming.TotalMinutes(session.Duration);
        if (session.ActualStartTime.Value.AddMinutes(totalMinutes) > now)
            return new(CompletionStatus.TooEarly);

        var pomos = SessionTiming.EarnedPomos(session.Duration);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var updated = await db.Sessions.Where(s => s.Id == sessionId && s.UserId == userId &&
            s.Status == SessionStatus.InProgress && s.LastActivityAt >= now - ActivityTimeout &&
            s.ActualStartTime == session.ActualStartTime && s.Duration == session.Duration)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, SessionStatus.Completed)
                .SetProperty(x => x.PomosEarned, pomos)
                .SetProperty(x => x.EndedAt, now));
        if (updated == 0)
        {
            var completed = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(s =>
                s.Id == sessionId && s.UserId == userId && s.Status == SessionStatus.Completed);
            return completed is null
                ? new(CompletionStatus.NotFound)
                : new(CompletionStatus.Completed, new SessionCompletion(completed.PomosEarned));
        }

        var usersUpdated = await db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.PomoBalance, u => u.PomoBalance + pomos));
        if (usersUpdated != 1)
            throw new InvalidOperationException("The session owner could not be updated.");
        await transaction.CommitAsync();
        return new(CompletionStatus.Completed, new SessionCompletion(pomos));
    }

    public async Task CancelExpiredAsync()
    {
        var cutoff = DateTime.UtcNow - ActivityTimeout;
        await db.Sessions.Where(s => s.Status == SessionStatus.InProgress &&
                                     (s.LastActivityAt == null || s.LastActivityAt < cutoff))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, SessionStatus.Cancelled));
    }
}
