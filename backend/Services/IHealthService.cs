using backend.DTOs;

namespace backend.Services;

public interface IHealthService
{
    HealthResponse GetStatus();
}
