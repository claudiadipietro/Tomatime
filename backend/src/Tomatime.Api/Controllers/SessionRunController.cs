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

    [HttpPost("extend-break")]
    public async Task<IActionResult> ExtendBreak(int sessionId, RewardPurchaseRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        var result = await sessions.ExtendBreakAsync(userId, sessionId, request.OperationId);
        return result.Status switch
        {
            RewardActionStatus.Success when result.State is not null => Ok(result.State),
            RewardActionStatus.NotFound => NotFound(),
            RewardActionStatus.InsufficientBalance => Conflict(new { message = "Insufficient balance." }),
            _ => BadRequest(new { message = "A break is not currently active." }),
        };
    }
}
