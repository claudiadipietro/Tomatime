using Tomatime.Api.DTOs;

namespace Tomatime.Api.Services;

public interface ISessionStartService
{
    Task<SpontaneousSessionStartResult> StartSpontaneousAsync(int userId, int duration);
    Task<bool> StartPlannedAsync(int userId, int sessionId);
}
