using backend.DTOs;

namespace backend.Services;

public sealed class HealthService : IHealthService
{
    public HealthResponse GetStatus()
    {
        return new HealthResponse("ok", DateTimeOffset.UtcNow);
    }
}
