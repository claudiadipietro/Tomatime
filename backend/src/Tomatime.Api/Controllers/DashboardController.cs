using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.DTOs;
using Tomatime.Api.Models;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IUserDatabaseService users;
    private readonly ISessionDatabaseService sessions;

    public DashboardController(IUserDatabaseService users, ISessionDatabaseService sessions)
    {
        this.users = users;
        this.sessions = sessions;
    }
    
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var user = await users.GetUserByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        var userSessions = await sessions.GetSessionsByUserIdAsync(userId);
        var now = DateTime.UtcNow;
        var next = userSessions
            .Where(session => session.Status == SessionStatus.Planned && session.PlannedStartTime >= now)
            .OrderBy(session => session.PlannedStartTime)
            .Select(session => new DashboardSession(session.PlannedStartTime, session.Duration))
            .FirstOrDefault();
        var completed = userSessions.Where(session => session.Status == SessionStatus.Completed).ToList();

        return Ok(new DashboardResponse(
            user.PomoBalance,
            next,
            completed.Count,
            completed.Sum(session => (long)session.Duration)));
    }
}
