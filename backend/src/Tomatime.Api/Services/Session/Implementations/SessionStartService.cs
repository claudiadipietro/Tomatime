using Tomatime.Api.DTOs;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class SessionStartService : ISessionStartService
{
    private readonly IUserDatabaseService _users;
    private readonly ISessionDatabaseService _sessions;

    public SessionStartService(IUserDatabaseService users, ISessionDatabaseService sessions)
    {
        _users = users;
        _sessions = sessions;
    }

    public async Task<SpontaneousSessionStartResult> StartSpontaneousAsync(int userId, int duration)
    {
        if (!SessionDuration.IsValid(duration))
        {
            return new(SpontaneousSessionStartStatus.InvalidDuration);
        }

        if (await _users.GetUserByIdAsync(userId) is null)
        {
            return new(SpontaneousSessionStartStatus.UserNotFound);
        }

        DateTime now = DateTime.UtcNow;
        Session session = new Session
        {
            UserId = userId,
            PlannedStartTime = now,
            ActualStartTime = now,
            LastActivityAt = now,
            Duration = duration,
            Status = SessionStatus.InProgress,
        };
        Session created = await _sessions.CreateSessionAsync(session)
            ?? throw new InvalidOperationException("Failed to create the session.");

        return new(
            SpontaneousSessionStartStatus.Started,
            new StartedSessionResponse(created.Id, created.Duration, created.ActualStartTime));
    }

    public Task<bool> StartPlannedAsync(int userId, int sessionId) =>
        _sessions.StartPlannedAsync(userId, sessionId, DateTime.UtcNow);
}
