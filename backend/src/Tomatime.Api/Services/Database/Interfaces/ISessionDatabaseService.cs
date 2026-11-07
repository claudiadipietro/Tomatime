using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public interface ISessionDatabaseService
{
    Task<Session?> GetSessionByIdAsync(int sessionId);
    Task<List<Session>> GetSessionsByUserIdAsync(int userId);
    Task<Session?> CreateSessionAsync(Session session);
    Task<Session?> UpdateSessionAsync(Session session);
    Task<bool> DeleteSessionAsync(int sessionId);
}