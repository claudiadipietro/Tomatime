using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.DTOs;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/session-plans")]
public sealed class SessionPlanController(ISessionPlanningService planningService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Plan(PlanSessionRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var result = await planningService.PlanAsync(userId, request.Duration, request.PlannedStartTime);
        return result.Status switch
        {
            SessionPlanningStatus.InvalidDuration => BadRequest(new { message = "Invalid duration." }),
            SessionPlanningStatus.InvalidStartTime => BadRequest(new { message = "Start time must be in the future." }),
            SessionPlanningStatus.UserNotFound => Unauthorized(),
            SessionPlanningStatus.Planned when result.Session is not null => Ok(result.Session),
            _ => throw new InvalidOperationException("Invalid session planning result."),
        };
    }
}
