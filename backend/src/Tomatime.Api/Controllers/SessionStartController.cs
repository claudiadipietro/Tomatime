using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.DTOs;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/session-starts")]
public sealed class SessionStartController(ISessionStartService sessionStartService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> StartSpontaneous(StartSessionRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        var result = await sessionStartService.StartSpontaneousAsync(userId, request.Duration);
        return result.Status switch
        {
            SpontaneousSessionStartStatus.InvalidDuration =>
                BadRequest(new { message = "Duration must be a multiple of 25 minutes." }),
            SpontaneousSessionStartStatus.UserNotFound => Unauthorized(),
            SpontaneousSessionStartStatus.Started when result.Session is not null => Ok(result.Session),
            _ => throw new InvalidOperationException("Invalid session start result."),
        };
    }

    [HttpPost("{sessionId:int}")]
    public async Task<IActionResult> StartPlanned(int sessionId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        if (!await sessionStartService.StartPlannedAsync(userId, sessionId))
            return NotFound(new { message = "Planned session not found or already started." });
        return NoContent();
    }
}
