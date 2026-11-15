using Tomatime.Api.DTOs;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class SessionStartService(
    IUserDatabaseService users,
    ISessionDatabaseService sessions) : ISessionStartService
{
    public async Task<SpontaneousSessionStartResult> StartSpontaneousAsync(int userId, int duration)
    {
        if (!SessionDuration.IsValid(duration))
            return new(SpontaneousSessionStartStatus.InvalidDuration);

        if (await users.GetUserByIdAsync(userId) is null)
            return new(SpontaneousSessionStartStatus.UserNotFound);

        var now = DateTime.UtcNow;
        var session = new Session
        {
            UserId = userId,
            PlannedStartTime = now,
            ActualStartTime = now,
            LastActivityAt = now,
            Duration = duration,
            Status = SessionStatus.InProgress,
        };
        var created = await sessions.CreateSessionAsync(session)
            ?? throw new InvalidOperationException("Failed to create the session.");

        return new(
            SpontaneousSessionStartStatus.Started,
            new StartedSessionResponse(created.Id, created.Duration, created.ActualStartTime));
    }

    public Task<bool> StartPlannedAsync(int userId, int sessionId) =>
        sessions.StartPlannedAsync(userId, sessionId, DateTime.UtcNow);
}
