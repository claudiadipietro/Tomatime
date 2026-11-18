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
    private readonly IUserDatabaseService _users;
    private readonly ISessionDatabaseService _sessions;

    public DashboardController(IUserDatabaseService users, ISessionDatabaseService sessions)
    {
        _users = users;
        _sessions = sessions;
    }
    
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        User? user = await _users.GetUserByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        List<Session> userSessions = await _sessions.GetSessionsByUserIdAsync(userId);
        DashboardSession? next = userSessions
            .Where(session => session.Status == SessionStatus.Planned)
            .OrderBy(session => session.PlannedStartTime)
            .Select(session => new DashboardSession(session.Id, session.PlannedStartTime, session.Duration))
            .FirstOrDefault();
        List<Session> completed = userSessions.Where(session => session.Status == SessionStatus.Completed).ToList();
        List<DashboardActiveSession> active = userSessions
            .Where(session => session.Status == SessionStatus.InProgress && session.ActualStartTime.HasValue)
            .OrderByDescending(session => session.ActualStartTime)
            .ThenByDescending(session => session.Id)
            .Select(session => new DashboardActiveSession(session.Id, session.ActualStartTime!.Value, session.Duration))
            .ToList();

        return Ok(new DashboardResponse(
            user.PomoBalance,
            next,
            completed.Count,
            completed.Sum(session => (long)session.Duration),
            active));
    }
}
