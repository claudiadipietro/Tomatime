using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
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
        var session = await _sessionService.GetSessionByIdAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        return Ok(session);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSessionsByUserId(int userId)
    {
        var sessions = await _sessionService.GetSessionsByUserIdAsync(userId);
        return Ok(sessions);
    }

}