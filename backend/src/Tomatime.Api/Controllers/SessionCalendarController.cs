using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/session-calendar")]
public sealed class SessionCalendarController(ISessionCalendarService calendar) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        if (from is null || to is null || to <= from || to - from > TimeSpan.FromDays(45))
            return BadRequest(new { message = "A valid date range of at most 45 days is required." });
        return Ok(await calendar.GetSessionsAsync(userId, from.Value.UtcDateTime, to.Value.UtcDateTime));
    }
}
