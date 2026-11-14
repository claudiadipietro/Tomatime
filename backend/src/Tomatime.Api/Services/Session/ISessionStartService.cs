using Tomatime.Api.DTOs;

namespace Tomatime.Api.Services;

public enum SpontaneousSessionStartStatus
{
    Started,
    InvalidDuration,
    UserNotFound,
}

public sealed record SpontaneousSessionStartResult(
    SpontaneousSessionStartStatus Status,
    StartedSessionResponse? Session = null);

public interface ISessionStartService
{
    Task<SpontaneousSessionStartResult> StartSpontaneousAsync(int userId, int duration);
    Task<bool> StartPlannedAsync(int userId, int sessionId);
}
