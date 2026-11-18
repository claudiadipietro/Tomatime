using Tomatime.Api.DTOs;

namespace Tomatime.Api.Services;

public interface ISessionRunService
{
    Task<ActiveSession?> HeartbeatAsync(int userId, int sessionId);
    Task<bool> CancelAsync(int userId, int sessionId);
    Task<CompletionResult> CompleteAsync(int userId, int sessionId);
    Task<RewardActionResult> ExtendBreakAsync(int userId, int sessionId, Guid operationId);
}
