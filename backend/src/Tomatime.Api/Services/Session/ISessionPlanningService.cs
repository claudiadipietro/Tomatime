using Tomatime.Api.DTOs;

namespace Tomatime.Api.Services;

public enum SessionPlanningStatus
{
    Planned,
    InvalidDuration,
    InvalidStartTime,
    UserNotFound,
}

public sealed record SessionPlanningResult(
    SessionPlanningStatus Status,
    PlannedSessionResponse? Session = null);

public interface ISessionPlanningService
{
    Task<SessionPlanningResult> PlanAsync(int userId, int duration, DateTimeOffset? plannedStartTime);
}
