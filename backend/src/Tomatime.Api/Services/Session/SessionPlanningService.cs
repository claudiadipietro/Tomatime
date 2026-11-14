using Tomatime.Api.DTOs;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class SessionPlanningService(
    IUserDatabaseService users,
    ISessionDatabaseService sessions) : ISessionPlanningService
{
    public async Task<SessionPlanningResult> PlanAsync(int userId, int duration, DateTimeOffset? plannedStartTime)
    {
        if (!SessionDuration.IsValid(duration))
            return new(SessionPlanningStatus.InvalidDuration);

        if (plannedStartTime is null || plannedStartTime <= DateTimeOffset.UtcNow)
            return new(SessionPlanningStatus.InvalidStartTime);

        if (await users.GetUserByIdAsync(userId) is null)
            return new(SessionPlanningStatus.UserNotFound);

        var session = new Session
        {
            UserId = userId,
            Duration = duration,
            PlannedStartTime = plannedStartTime.Value.UtcDateTime,
            Status = SessionStatus.Planned,
        };
        var created = await sessions.CreateSessionAsync(session)
            ?? throw new InvalidOperationException("Failed to plan the session.");

        return new(SessionPlanningStatus.Planned,
            new PlannedSessionResponse(created.Id, created.Duration, created.PlannedStartTime));
    }
}
