using Tomatime.Api.DTOs;

namespace Tomatime.Api.Services;

public interface ISessionPlanningService
{
    Task<SessionPlanningResult> PlanAsync(int userId, int duration, DateTimeOffset? plannedStartTime);
}
