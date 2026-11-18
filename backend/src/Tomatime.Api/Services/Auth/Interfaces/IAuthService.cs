using Tomatime.Api.DTOs;

namespace Tomatime.Api.Services;

public interface IAuthService
{
    Task<AuthSessionTokens?> SignupAsync(SignupRequest request);
    Task<AuthSessionTokens?> LoginAsync(LoginRequest request);
    Task<AuthSessionTokens?> RefreshAsync(string? refreshToken);
    Task RevokeRefreshTokenAsync(string? refreshToken);
    Task RequestPasswordResetAsync(ForgotPasswordRequest request);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
}
