using Microsoft.EntityFrameworkCore;
using Tomatime.Api.Context;
using Tomatime.Api.DTOs;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class SessionCalendarService(AppDbContext db) : ISessionCalendarService
{
    public async Task<List<CalendarSession>> GetSessionsAsync(int userId, DateTime from, DateTime to)
    {
        var sessions = await db.Sessions.AsNoTracking()
            .Where(s => s.UserId == userId &&
                ((s.Status == SessionStatus.Planned ? s.PlannedStartTime : (s.ActualStartTime ?? s.PlannedStartTime)) >= from) &&
                ((s.Status == SessionStatus.Planned ? s.PlannedStartTime : (s.ActualStartTime ?? s.PlannedStartTime)) < to))
            .OrderBy(s => s.Status == SessionStatus.Planned ? s.PlannedStartTime : (s.ActualStartTime ?? s.PlannedStartTime))
            .ThenBy(s => s.Id)
            .Select(s => new { s.Id, s.Duration, s.Status, s.PlannedStartTime, s.ActualStartTime, s.PomosEarned })
            .ToListAsync();
        return sessions.Select(s => new CalendarSession(s.Id, s.Duration, s.Status.ToString(),
            s.PlannedStartTime, s.ActualStartTime, s.PomosEarned)).ToList();
    }
}
