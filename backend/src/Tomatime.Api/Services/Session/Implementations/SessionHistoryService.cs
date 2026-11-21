using Microsoft.EntityFrameworkCore;
using Tomatime.Api.Context;
using Tomatime.Api.DTOs;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class SessionHistoryService : ISessionHistoryService
{
    private readonly AppDbContext _db;

    public SessionHistoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SessionHistoryItem?> GetHistoryItemAsync(int userId, int sessionId)
    {
        var session = await _db.Sessions.AsNoTracking()
            .Where(s => s.Id == sessionId && s.UserId == userId &&
                (s.Status == SessionStatus.Completed || s.Status == SessionStatus.Cancelled))
            .Select(s => new
            {
                s.Id, s.Duration, s.Status, s.ActualStartTime, s.EndedAt, s.PomosEarned, s.PlannedStartTime,
            }).SingleOrDefaultAsync();
        return session is null ? null : new SessionHistoryItem(
            session.Id, session.Duration, session.Status.ToString(),
            session.ActualStartTime, session.EndedAt, session.PomosEarned, session.PlannedStartTime);
    }

    public async Task<List<SessionHistoryItem>> GetHistoryAsync(int userId)
    {
        var sessions = await _db.Sessions.AsNoTracking()
            .Where(s => s.UserId == userId &&
                (s.Status == SessionStatus.Completed || s.Status == SessionStatus.Cancelled))
            .OrderByDescending(s => s.ActualStartTime ?? s.PlannedStartTime)
            .ThenByDescending(s => s.Id)
            .Select(s => new
            {
                s.Id, s.Duration, s.Status, s.ActualStartTime, s.EndedAt, s.PomosEarned, s.PlannedStartTime,
            })
            .ToListAsync();
        return sessions.Select(s => new SessionHistoryItem(
            s.Id, s.Duration, s.Status.ToString(), s.ActualStartTime, s.EndedAt,
            s.PomosEarned, s.PlannedStartTime)).ToList();
    }
}
