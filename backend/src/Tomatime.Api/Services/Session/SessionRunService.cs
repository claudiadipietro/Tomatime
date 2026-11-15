using Microsoft.EntityFrameworkCore;
using Tomatime.Api.Context;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class SessionRunService(AppDbContext db) : ISessionRunService
{
    private static readonly TimeSpan ActivityTimeout = TimeSpan.FromMinutes(2);

    public async Task<ActiveSession?> HeartbeatAsync(int userId, int sessionId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var session = await db.Sessions.FromSqlInterpolated($"SELECT * FROM \"Sessions\" WHERE \"Id\" = {sessionId} AND \"UserId\" = {userId} FOR UPDATE").SingleOrDefaultAsync();
        if (session?.Status != SessionStatus.InProgress || session.ActualStartTime is null ||
            session.LastActivityAt is null)
            return null;
        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE").SingleAsync();
        var now = DateTime.UtcNow;
        if (session.LastActivityAt < now - ActivityTimeout) return null;
        if (session.Duration > 25 && now >= session.ActualStartTime.Value.AddMinutes(25) && !session.StoredBreakApplied)
        {
            var pending = await db.RewardOperations.Where(o => o.UserId == userId && o.RewardId == RewardCatalog.ExtraBreak && o.Kind == "extra-break" &&
                o.SessionId == null && o.CreatedAt <= session.ActualStartTime.Value)
                .OrderBy(o => o.CreatedAt).ThenBy(o => o.Id).FirstOrDefaultAsync();
            if (pending is not null)
            {
                var extensions = (int[])session.BreakExtensions.Clone();
                if (extensions.Length == 0) extensions = new int[session.Duration / 25 - 1];
                extensions[0]++;
                session.BreakExtensions = extensions;
                session.StoredBreakApplied = true;
                pending.SessionId = session.Id;
            }
        }
        session.LastActivityAt = now;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new ActiveSession(session.Id, session.Duration, session.ActualStartTime.Value, now, session.BreakExtensions, user.PomoBalance);
    }

    public async Task<RewardActionResult> ExtendBreakAsync(int userId, int sessionId, Guid operationId)
    {
        if (operationId == Guid.Empty) return new(RewardActionStatus.InvalidOperation);
        await using var tx = await db.Database.BeginTransactionAsync();
        var session = await db.Sessions.FromSqlInterpolated($"SELECT * FROM \"Sessions\" WHERE \"Id\" = {sessionId} AND \"UserId\" = {userId} FOR UPDATE").SingleOrDefaultAsync();
        if (session is null) return new(RewardActionStatus.NotFound);
        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE").SingleAsync();
        var now = DateTime.UtcNow;
        var previous = await db.RewardOperations.AsNoTracking().SingleOrDefaultAsync(o => o.Id == operationId);
        async Task<RewardShopState> state() => await RewardShopService.StateAsync(db, user);
        if (previous is not null)
            return previous.UserId == userId && previous.RewardId == RewardCatalog.ExtraBreak && previous.Kind == "extend-break" && previous.SessionId == sessionId
                ? new(RewardActionStatus.Success, await state()) : new(RewardActionStatus.InvalidOperation);
        if (session.Status != SessionStatus.InProgress || session.ActualStartTime is null ||
            session.LastActivityAt is null || session.LastActivityAt < now - ActivityTimeout)
            return new(RewardActionStatus.NotFound);
        var phase = BreakSchedule.At(session.Duration, session.BreakExtensions, now - session.ActualStartTime.Value);
        if (!phase.InBreak) return new(RewardActionStatus.InvalidOperation);
        var reward = await db.Rewards.SingleAsync(r => r.Id == RewardCatalog.ExtraBreak);
        if (user.PomoBalance < reward.Cost) return new(RewardActionStatus.InsufficientBalance);
        var extensions = (int[])session.BreakExtensions.Clone();
        if (extensions.Length == 0) extensions = new int[session.Duration / 25 - 1];
        extensions[phase.BreakIndex]++;
        session.BreakExtensions = extensions;
        user.PomoBalance -= reward.Cost;
        db.RewardOperations.Add(new Models.RewardOperation { Id = operationId, UserId = userId, RewardId = reward.Id, Kind = "extend-break", SessionId = sessionId, CreatedAt = now });
        await db.SaveChangesAsync();
        var result = await state();
        await tx.CommitAsync();
        return new(RewardActionStatus.Success, result);
    }

    public async Task<bool> CancelAsync(int userId, int sessionId) =>
        await db.Sessions.Where(s => s.Id == sessionId && s.UserId == userId &&
                                     s.Status == SessionStatus.InProgress)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, SessionStatus.Cancelled)
                .SetProperty(x => x.EndedAt, DateTime.UtcNow)) != 0;

    public async Task<CompletionResult> CompleteAsync(int userId, int sessionId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var session = await db.Sessions.FromSqlInterpolated($"SELECT * FROM \"Sessions\" WHERE \"Id\" = {sessionId} AND \"UserId\" = {userId} FOR UPDATE").SingleOrDefaultAsync();
        var now = DateTime.UtcNow;
        if (session?.Status == SessionStatus.Completed)
            return new(CompletionStatus.Completed, new SessionCompletion(session.PomosEarned));
        if (session?.Status != SessionStatus.InProgress)
            return new(CompletionStatus.NotFound);
        if (!SessionDuration.IsValid(session.Duration))
        {
            await CancelAsync(userId, sessionId);
            await transaction.CommitAsync();
            return new(CompletionStatus.NotFound);
        }
        if (session?.ActualStartTime is null || session.LastActivityAt is null ||
            session.LastActivityAt < now - ActivityTimeout)
        {
            return new(CompletionStatus.NotFound);
        }

        var totalMinutes = BreakSchedule.TotalMinutes(session.Duration, session.BreakExtensions);
        if (session.ActualStartTime.Value.AddMinutes(totalMinutes) > now)
            return new(CompletionStatus.TooEarly);

        var pomos = SessionTiming.EarnedPomos(session.Duration);
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
