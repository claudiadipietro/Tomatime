namespace Tomatime.Api.Services;

public sealed record ActiveSession(int Id, int Duration, DateTime ActualStartTime, DateTime ServerTime, int[] BreakExtensions, int PomoBalance);
public sealed record SessionCompletion(int PomosEarned);
public enum CompletionStatus { Completed, NotFound, TooEarly }
public sealed record CompletionResult(CompletionStatus Status, SessionCompletion? Completion = null);

public interface ISessionRunService
{
    Task<ActiveSession?> HeartbeatAsync(int userId, int sessionId);
    Task<bool> CancelAsync(int userId, int sessionId);
    Task<CompletionResult> CompleteAsync(int userId, int sessionId);
    Task<RewardActionResult> ExtendBreakAsync(int userId, int sessionId, Guid operationId);
}
