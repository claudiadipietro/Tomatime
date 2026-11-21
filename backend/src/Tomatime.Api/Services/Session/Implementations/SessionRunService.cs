using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tomatime.Api.Context;
using Tomatime.Api.DTOs;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class SessionRunService : ISessionRunService
{
    private readonly AppDbContext _db;
    private readonly SessionTestClock _clock;

    public SessionRunService(AppDbContext db, SessionTestClock? clock = null)
    {
        _db = db;
        _clock = clock ?? new SessionTestClock(false);
    }

    public async Task<ActiveSession?> HeartbeatAsync(int userId, int sessionId)
    {
        await using IDbContextTransaction tx = await _db.Database.BeginTransactionAsync();
        Session? session = await _db.Sessions.FromSqlInterpolated($"SELECT * FROM \"Sessions\" WHERE \"Id\" = {sessionId} AND \"UserId\" = {userId} FOR UPDATE").SingleOrDefaultAsync();
        if (session?.Status != SessionStatus.InProgress || session.ActualStartTime is null)
        {
            return null;
        }

        User user = await _db.Users.FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE").SingleAsync();
        DateTime now = _clock.UtcNow(sessionId);
        await ApplyStoredBreakAsync(session, userId, now);
        int extraBreakCost = await _db.Rewards.Where(reward => reward.Id == RewardCatalog.ExtraBreak)
            .Select(reward => reward.Cost).SingleAsync();
        session.LastActivityAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return new ActiveSession(session.Id, session.Duration, session.ActualStartTime.Value, now, session.BreakExtensions, user.PomoBalance, extraBreakCost);
    }

    public async Task<RewardActionResult> ExtendBreakAsync(int userId, int sessionId, Guid operationId)
    {
        if (operationId == Guid.Empty)
        {
            return new(RewardActionStatus.InvalidOperation);
        }

        await using IDbContextTransaction tx = await _db.Database.BeginTransactionAsync();
        Session? session = await _db.Sessions.FromSqlInterpolated($"SELECT * FROM \"Sessions\" WHERE \"Id\" = {sessionId} AND \"UserId\" = {userId} FOR UPDATE").SingleOrDefaultAsync();
        if (session is null)
        {
            return new(RewardActionStatus.NotFound);
        }

        User user = await _db.Users.FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE").SingleAsync();
        DateTime now = _clock.UtcNow(sessionId);
        UserReward? previous = await _db.UserRewards.AsNoTracking().SingleOrDefaultAsync(o => o.OperationId == operationId);
        async Task<RewardShopState> state() => await RewardShopService.StateAsync(_db, user);
        if (previous is not null)
        {
            return previous.UserId == userId && previous.RewardId == RewardCatalog.ExtraBreak && previous.Kind == "extend-break" && previous.SessionId == sessionId
                ? new(RewardActionStatus.Success, await state()) : new(RewardActionStatus.InvalidOperation);
        }

        if (session.Status != SessionStatus.InProgress || session.ActualStartTime is null)
        {
            return new(RewardActionStatus.NotFound);
        }

        await ApplyStoredBreakAsync(session, userId, now);
        (int BreakIndex, bool InBreak) phase = BreakSchedule.At(session.Duration, session.BreakExtensions, now - session.ActualStartTime.Value);
        if (!phase.InBreak || session.BreakExtensions.ElementAtOrDefault(phase.BreakIndex) > 0)
        {
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return new(RewardActionStatus.InvalidOperation);
        }

        Reward reward = await _db.Rewards.SingleAsync(r => r.Id == RewardCatalog.ExtraBreak);
        if (user.PomoBalance < reward.Cost)
        {
            return new(RewardActionStatus.InsufficientBalance);
        }

        int[] extensions = (int[])session.BreakExtensions.Clone();
        if (extensions.Length == 0)
        {
            extensions = new int[session.Duration / 25 - 1];
        }

        extensions[phase.BreakIndex]++;
        session.BreakExtensions = extensions;
        user.PomoBalance -= reward.Cost;
        _db.UserRewards.Add(new UserReward { OperationId = operationId, UserId = userId, RewardId = reward.Id, Kind = "extend-break", SessionId = sessionId, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        RewardShopState result = await state();
        await tx.CommitAsync();
        return new(RewardActionStatus.Success, result);
    }

    public async Task<bool> CancelAsync(int userId, int sessionId) =>
        await _db.Sessions.Where(s => s.Id == sessionId && s.UserId == userId &&
                                     (s.Status == SessionStatus.Planned || s.Status == SessionStatus.InProgress))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, SessionStatus.Cancelled)
                .SetProperty(x => x.EndedAt, DateTime.UtcNow)) != 0;

    public async Task<CompletionResult> CompleteAsync(int userId, int sessionId)
    {
        await using IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync();
        Session? session = await _db.Sessions.FromSqlInterpolated($"SELECT * FROM \"Sessions\" WHERE \"Id\" = {sessionId} AND \"UserId\" = {userId} FOR UPDATE").SingleOrDefaultAsync();
        DateTime now = _clock.UtcNow(sessionId);
        if (session?.Status == SessionStatus.Completed)
        {
            return new(CompletionStatus.Completed, new SessionCompletion(session.PomosEarned));
        }

        if (session?.Status != SessionStatus.InProgress)
        {
            return new(CompletionStatus.NotFound);
        }

        if (!SessionDuration.IsValid(session.Duration))
        {
            await CancelAsync(userId, sessionId);
            await transaction.CommitAsync();
            return new(CompletionStatus.NotFound);
        }
        if (session.ActualStartTime is null)
        {
            return new(CompletionStatus.NotFound);
        }

        await _db.Users.FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE").SingleAsync();
        await ApplyStoredBreakAsync(session, userId, now);
        int totalMinutes = BreakSchedule.TotalMinutes(session.Duration, session.BreakExtensions);
        if (session.ActualStartTime.Value.AddMinutes(totalMinutes) > now)
        {
            return new(CompletionStatus.TooEarly);
        }

        await _db.SaveChangesAsync();
        int pomos = SessionTiming.EarnedPomos(session.Duration);
        int updated = await _db.Sessions.Where(s => s.Id == sessionId && s.UserId == userId &&
            s.Status == SessionStatus.InProgress &&
            s.ActualStartTime == session.ActualStartTime && s.Duration == session.Duration)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, SessionStatus.Completed)
                .SetProperty(x => x.PomosEarned, pomos)
                .SetProperty(x => x.EndedAt, DateTime.UtcNow));
        if (updated == 0)
        {
            var completed = await _db.Sessions.AsNoTracking().SingleOrDefaultAsync(s =>
                s.Id == sessionId && s.UserId == userId && s.Status == SessionStatus.Completed);
            return completed is null
                ? new(CompletionStatus.NotFound)
                : new(CompletionStatus.Completed, new SessionCompletion(completed.PomosEarned));
        }

        int usersUpdated = await _db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.PomoBalance, u => u.PomoBalance + pomos));
        if (usersUpdated != 1)
        {
            throw new InvalidOperationException("The session owner could not be updated.");
        }

        await transaction.CommitAsync();
        return new(CompletionStatus.Completed, new SessionCompletion(pomos));
    }

    private async Task ApplyStoredBreakAsync(Models.Session session, int userId, DateTime now)
    {
        if (session.Duration <= 25 || session.StoredBreakApplied ||
            session.BreakExtensions.ElementAtOrDefault(0) > 0 ||
            session.ActualStartTime is null || now < session.ActualStartTime.Value.AddMinutes(25))
        {
            return;
        }

        UserReward? pending = await _db.UserRewards.Where(o => o.UserId == userId && o.RewardId == RewardCatalog.ExtraBreak && o.Kind == "extra-break" &&
            o.SessionId == null && o.CreatedAt <= session.ActualStartTime.Value)
            .OrderBy(o => o.CreatedAt).ThenBy(o => o.OperationId).FirstOrDefaultAsync();
        if (pending is null)
        {
            return;
        }

        int[] extensions = (int[])session.BreakExtensions.Clone();
        if (extensions.Length == 0)
        {
            extensions = new int[session.Duration / 25 - 1];
        }

        extensions[0]++;
        session.BreakExtensions = extensions;
        session.StoredBreakApplied = true;
        pending.SessionId = session.Id;
    }
}
