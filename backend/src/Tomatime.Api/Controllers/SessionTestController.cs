using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tomatime.Api.Context;
using Tomatime.Api.Models;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/testing/sessions")]
public sealed class SessionTestController(
    AppDbContext db, SessionTestClock clock, ISessionRunService runs) : ControllerBase
{
    public sealed record AdvanceRequest([param: Range(1, 3600)] int Seconds);

    [HttpPost("{sessionId:int}/advance")]
    public async Task<IActionResult> Advance(int sessionId, AdvanceRequest request)
    {
        if (!clock.Enabled)
        {
            return NotFound();
        }
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            Session? session = await db.Sessions.FromSqlInterpolated(
                $"SELECT * FROM \"Sessions\" WHERE \"Id\" = {sessionId} AND \"UserId\" = {userId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (session?.Status != SessionStatus.InProgress || session.ActualStartTime is null)
            {
                return NotFound();
            }
            clock.Advance(sessionId, request.Seconds);
            await transaction.CommitAsync();
        }
        var active = await runs.HeartbeatAsync(userId, sessionId);
        return active is null ? NotFound() : Ok(active);
    }
}
