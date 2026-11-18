using Tomatime.Api.DTOs;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class SessionPlanningService : ISessionPlanningService
{
    private readonly IUserDatabaseService _users;
    private readonly ISessionDatabaseService _sessions;
    private readonly ReminderWakeup? _reminders;

    public SessionPlanningService(
        IUserDatabaseService users,
        ISessionDatabaseService sessions,
        ReminderWakeup? reminders = null)
    {
        _users = users;
        _sessions = sessions;
        _reminders = reminders;
    }

    public async Task<SessionPlanningResult> PlanAsync(int userId, int duration, DateTimeOffset? plannedStartTime)
    {
        if (!SessionDuration.IsValid(duration))
        {
            return new(SessionPlanningStatus.InvalidDuration);
        }

        if (plannedStartTime is null || plannedStartTime <= DateTimeOffset.UtcNow)
        {
            return new(SessionPlanningStatus.InvalidStartTime);
        }

        if (await _users.GetUserByIdAsync(userId) is null)
        {
            return new(SessionPlanningStatus.UserNotFound);
        }

        Session session = new Session
        {
            UserId = userId,
            Duration = duration,
            PlannedStartTime = plannedStartTime.Value.UtcDateTime,
            Status = SessionStatus.Planned,
        };
        Session created = await _sessions.CreateSessionAsync(session)
            ?? throw new InvalidOperationException("Failed to plan the session.");

        _reminders?.Trigger();
        return new(SessionPlanningStatus.Planned,
            new PlannedSessionResponse(created.Id, created.Duration, created.PlannedStartTime));
    }
}
