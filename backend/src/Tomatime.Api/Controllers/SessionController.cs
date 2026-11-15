using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sessions")] 
public class SessionController : ControllerBase
{
    private readonly ISessionDatabaseService _sessionService;

    public SessionController(ISessionDatabaseService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet("{sessionId}")]
    public async Task<IActionResult> GetSessionById(int sessionId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        var session = await _sessionService.GetSessionByIdAsync(sessionId);
        if (session == null || session.UserId != userId)
        {
            return NotFound();
        }

        return Ok(session);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSessionsByUserId([FromQuery] int? userId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var authenticatedUserId) ||
            (userId.HasValue && userId != authenticatedUserId))
            return Unauthorized();
        var sessions = await _sessionService.GetSessionsByUserIdAsync(authenticatedUserId);
        return Ok(sessions);
    }

}