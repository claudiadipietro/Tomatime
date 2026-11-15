using Microsoft.EntityFrameworkCore;
using Tomatime.Api.Context;
using Tomatime.Api.DTOs;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class SessionHistoryService(AppDbContext db) : ISessionHistoryService
{
    public async Task<List<SessionHistoryItem>> GetHistoryAsync(int userId)
    {
        var sessions = await db.Sessions.AsNoTracking()
            .Where(s => s.UserId == userId &&
                (s.Status == SessionStatus.Completed || s.Status == SessionStatus.Cancelled))
            .OrderByDescending(s => s.ActualStartTime ?? s.PlannedStartTime)
            .ThenByDescending(s => s.Id)
            .Select(s => new
            {
                s.Id, s.Duration, s.Status, s.ActualStartTime, s.EndedAt, s.PomosEarned,
            })
            .ToListAsync();
        return sessions.Select(s => new SessionHistoryItem(
            s.Id, s.Duration, s.Status.ToString(), s.ActualStartTime, s.EndedAt,
            s.PomosEarned)).ToList();
    }
}
