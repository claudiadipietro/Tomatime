using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/session-history")]
public sealed class SessionHistoryController(ISessionHistoryService history) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        return Ok(await history.GetHistoryAsync(userId));
    }

    [HttpGet("{sessionId:int}")]
    public async Task<IActionResult> GetItem(int sessionId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        var session = await history.GetHistoryItemAsync(userId, sessionId);
        return session is null ? NotFound() : Ok(session);
    }
}
