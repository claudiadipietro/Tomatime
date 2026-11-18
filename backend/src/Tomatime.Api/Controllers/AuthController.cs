using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.DTOs;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Signup(SignupRequest request)
    {
        AuthResponse? response = await _authService.SignupAsync(request);

        if (response is null)
        {
            return Conflict(new { message = "An account with that email already exists." });
        }

        return Ok(response);
    }

    [HttpPost("/api/auth/login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        AuthResponse? response = await _authService.LoginAsync(request);

        if (response is null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(response);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await _authService.RequestPasswordResetAsync(request);

        return Ok(new
        {
            message = "If the email exists, we will try to send password reset instructions.",
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        bool reset = await _authService.ResetPasswordAsync(request);

        return reset
            ? Ok(new { message = "Password reset successfully." })
            : BadRequest(new { message = "The reset token is invalid or expired." });
    }
}
