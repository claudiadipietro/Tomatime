using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Models;
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

    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] Session session)
    {
        var createdSession = await _sessionService.CreateSessionAsync(session);
        if (createdSession == null)
        {
            return BadRequest("Failed to create session.");
        }
        return Ok(createdSession);
    }

    [HttpPut("{sessionId}")]
    public async Task<IActionResult> UpdateSession(int sessionId, [FromBody] Session session)
    {
        if (sessionId != session.Id)
        {
            return BadRequest("Session ID mismatch.");
        }

        var updatedSession = await _sessionService.UpdateSessionAsync(session);
        if (updatedSession == null)
        {
            return BadRequest("Failed to update session.");
        }

        return Ok(updatedSession);
    }
}