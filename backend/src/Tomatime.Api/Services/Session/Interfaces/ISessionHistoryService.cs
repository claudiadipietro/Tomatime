using Tomatime.Api.DTOs;

namespace Tomatime.Api.Services;

public interface ISessionHistoryService
{
    Task<List<SessionHistoryItem>> GetHistoryAsync(int userId);
    Task<SessionHistoryItem?> GetHistoryItemAsync(int userId, int sessionId);
}
