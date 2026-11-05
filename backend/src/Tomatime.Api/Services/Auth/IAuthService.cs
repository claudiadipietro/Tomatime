using Tomatime.Api.DTOs;

namespace Tomatime.Api.Services;

public interface IAuthService
{
    Task<AuthResponse?> SignupAsync(SignupRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
}
