using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Services;
using Tomatime.Api.DTOs;
using Tomatime.Api.Models;

namespace Tomatime.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sessions")] 
public class SessionController : ControllerBase
{
    private readonly ISessionDatabaseService _sessionService;
    private readonly ISessionCalendarService _calendar;
    private readonly ISessionHistoryService _history;
    private readonly ISessionPlanningService _planning;
    private readonly ISessionStartService _start;
    private readonly ISessionRunService _run;

    public SessionController(
        ISessionDatabaseService sessionService,
        ISessionCalendarService calendar,
        ISessionHistoryService history,
        ISessionPlanningService planning,
        ISessionStartService start,
        ISessionRunService run)
    {
        _sessionService = sessionService;
        _calendar = calendar;
        _history = history;
        _planning = planning;
        _start = start;
        _run = run;
    }

    [HttpGet("{sessionId:int}")]
    public async Task<IActionResult> GetSessionById(int sessionId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        Session? session = await _sessionService.GetSessionByIdAsync(sessionId);
        if (session == null || session.UserId != userId)
        {
            return NotFound();
        }

        return Ok(session);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSessionsByUserId([FromQuery] int? userId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int authenticatedUserId) ||
            (userId.HasValue && userId != authenticatedUserId))
        {
            return Unauthorized();
        }

        List<Session> sessions = await _sessionService.GetSessionsByUserIdAsync(authenticatedUserId);
        return Ok(sessions);
    }

    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        if (from is null || to is null || to <= from || to - from > TimeSpan.FromDays(45))
        {
            return BadRequest(new { message = "A valid date range of at most 45 days is required." });
        }

        return Ok(await _calendar.GetSessionsAsync(userId, from.Value.UtcDateTime, to.Value.UtcDateTime));
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        return Ok(await _history.GetHistoryAsync(userId));
    }

    [HttpGet("history/{sessionId:int}")]
    public async Task<IActionResult> GetHistoryItem(int sessionId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        SessionHistoryItem? session = await _history.GetHistoryItemAsync(userId, sessionId);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost]
    public Task<IActionResult> Create(CreateSessionRequest request) =>
        request.PlannedStartTime is null
            ? StartSpontaneous(new StartSessionRequest { Duration = request.Duration })
            : Plan(new PlanSessionRequest { Duration = request.Duration, PlannedStartTime = request.PlannedStartTime });


    [NonAction]
    public async Task<IActionResult> Plan(PlanSessionRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        SessionPlanningResult result = await _planning.PlanAsync(userId, request.Duration, request.PlannedStartTime);
        return result.Status switch
        {
            SessionPlanningStatus.InvalidDuration => BadRequest(new { message = "Invalid duration." }),
            SessionPlanningStatus.InvalidStartTime => BadRequest(new { message = "Start time must be in the future." }),
            SessionPlanningStatus.UserNotFound => Unauthorized(),
            SessionPlanningStatus.Planned when result.Session is not null => Ok(result.Session),
            _ => throw new InvalidOperationException("Invalid session planning result."),
        };
    }

    [NonAction]
    public async Task<IActionResult> StartSpontaneous(StartSessionRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        SpontaneousSessionStartResult result = await _start.StartSpontaneousAsync(userId, request.Duration);
        return result.Status switch
        {
            SpontaneousSessionStartStatus.InvalidDuration =>
                BadRequest(new { message = "Duration must be a multiple of 25 minutes." }),
            SpontaneousSessionStartStatus.UserNotFound => Unauthorized(),
            SpontaneousSessionStartStatus.Started when result.Session is not null => Ok(result.Session),
            _ => throw new InvalidOperationException("Invalid session start result."),
        };
    }

    [HttpPost("{sessionId:int}/start")]
    public async Task<IActionResult> StartPlanned(int sessionId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        if (!await _start.StartPlannedAsync(userId, sessionId))
        {
            return NotFound(new { message = "Planned session not found or already started." });
        }

        return NoContent();
    }

    [HttpPost("{sessionId:int}/heartbeat")]
    public async Task<IActionResult> Heartbeat(int sessionId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        ActiveSession? session = await _run.HeartbeatAsync(userId, sessionId);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("{sessionId:int}/cancel")]
    public async Task<IActionResult> Cancel(int sessionId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        return await _run.CancelAsync(userId, sessionId) ? NoContent() : NotFound();
    }

    [HttpPost("{sessionId:int}/complete")]
    public async Task<IActionResult> Complete(int sessionId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        CompletionResult result = await _run.CompleteAsync(userId, sessionId);
        return result.Status switch
        {
            CompletionStatus.Completed when result.Completion is not null => Ok(result.Completion),
            CompletionStatus.TooEarly => Conflict(new { message = "The session is not finished yet." }),
            _ => NotFound(),
        };
    }

    [HttpPost("{sessionId:int}/extra-break")]
    public async Task<IActionResult> ExtendBreak(int sessionId, RewardPurchaseRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        RewardActionResult result = await _run.ExtendBreakAsync(userId, sessionId, request.OperationId);
        return result.Status switch
        {
            RewardActionStatus.Success when result.State is not null => Ok(result.State),
            RewardActionStatus.NotFound => NotFound(),
            RewardActionStatus.InsufficientBalance => Conflict(new { message = "Insufficient balance." }),
            _ => BadRequest(new { message = "A break is not currently active." }),
        };
    }
}