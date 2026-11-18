using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.DTOs;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "__Secure-tomatime-refresh";
    private const string RefreshRequestHeaderName = "X-Tomatime-Refresh";
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Signup(SignupRequest request)
    {
        AuthSessionTokens? session = await _authService.SignupAsync(request);

        if (session is null)
        {
            return Conflict(new { message = "An account with that email already exists." });
        }

        SetRefreshCookie(session);
        return Ok(session.Response);
    }

    [HttpPost("/api/auth/login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        AuthSessionTokens? session = await _authService.LoginAsync(request);

        if (session is null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        SetRefreshCookie(session);
        return Ok(session.Response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh()
    {
        if (Request.Headers[RefreshRequestHeaderName] != "1")
        {
            return BadRequest();
        }

        AuthSessionTokens? session = await _authService.RefreshAsync(
            Request.Cookies[RefreshTokenCookieName]);

        if (session is null)
        {
            ClearRefreshCookie();
            return Unauthorized();
        }

        SetRefreshCookie(session);
        return Ok(session.Response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Headers[RefreshRequestHeaderName] != "1")
        {
            return BadRequest();
        }

        await _authService.RevokeRefreshTokenAsync(Request.Cookies[RefreshTokenCookieName]);
        ClearRefreshCookie();
        return NoContent();
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

    private void SetRefreshCookie(AuthSessionTokens session)
    {
        Response.Cookies.Append(RefreshTokenCookieName, session.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
            Expires = session.RefreshTokenExpiresAt,
        });
    }

    private void ClearRefreshCookie()
    {
        Response.Cookies.Append(RefreshTokenCookieName, string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
            Expires = DateTimeOffset.UnixEpoch,
            MaxAge = TimeSpan.Zero,
        });
    }
}
