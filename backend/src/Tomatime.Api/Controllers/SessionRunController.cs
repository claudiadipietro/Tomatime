using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/session-runs/{sessionId:int}")]
public sealed class SessionRunController(ISessionRunService sessions) : ControllerBase
{
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat(int sessionId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        var session = await sessions.HeartbeatAsync(userId, sessionId);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(int sessionId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        return await sessions.CancelAsync(userId, sessionId) ? NoContent() : NotFound();
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete(int sessionId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        var result = await sessions.CompleteAsync(userId, sessionId);
        return result.Status switch
        {
            CompletionStatus.Completed when result.Completion is not null => Ok(result.Completion),
            CompletionStatus.TooEarly => Conflict(new { message = "The session is not finished yet." }),
            _ => NotFound(),
        };
    }
}
