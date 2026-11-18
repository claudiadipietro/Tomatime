using Tomatime.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Tomatime.Api.Services;

public class SessionDatabaseService : ISessionDatabaseService
{
    private readonly IRepository<Session> _repository;

    public SessionDatabaseService(IRepository<Session> repository)
    {
        _repository = repository;
    }

    public async Task<Session?> GetSessionByIdAsync(int sessionId)
    {
        return await _repository.GetByIdAsync(sessionId);
    }

    public async Task<Session?> CreateSessionAsync(Session session)
    {
        await _repository.AddAsync(session);
        await _repository.SaveChangesAsync();
        return session;
    }

    public async Task<Session?> UpdateSessionAsync(Session session)
    {
        Session? existingSession = await _repository.GetByIdAsync(session.Id);
        if (existingSession is null)
        {
            return null;
        }

        existingSession.ActualStartTime = session.ActualStartTime;
        existingSession.Duration = session.Duration;
        existingSession.Status = session.Status;

        await _repository.SaveChangesAsync();
        return existingSession;
    }

    public async Task<bool> DeleteSessionAsync(int sessionId)
    {
        Session? session = await _repository.GetByIdAsync(sessionId);
        if (session is null)
        {
            return false;
        }

        _repository.Remove(session);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> StartPlannedAsync(int userId, int sessionId, DateTime startedAt)
    {
        int updated = await _repository.Query()
            .Where(session => session.Id == sessionId && session.UserId == userId &&
                              session.Status == SessionStatus.Planned)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.Status, SessionStatus.InProgress)
                .SetProperty(session => session.ActualStartTime, startedAt)
                .SetProperty(session => session.LastActivityAt, startedAt));
        return updated != 0;
    }

    public Task<List<Session>> GetSessionsByUserIdAsync(int userId)
    {
        return _repository.Query()
            .Where(session => session.UserId == userId)
            .ToListAsync();
    }
}