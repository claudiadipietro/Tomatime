using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Tomatime.Api.DTOs;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class AuthService: IAuthService
{
    private readonly IUserDatabaseService _userDatabaseService;
    private readonly IRepository<PasswordResetToken> _passwordResetTokenRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IPasswordResetEmailSender _passwordResetEmailSender;
    private readonly IConfiguration _configuration;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromMinutes(30);

    public AuthService(
        IUserDatabaseService userDatabaseService,
        IRepository<PasswordResetToken> passwordResetTokenRepository,
        IPasswordHasher<User> passwordHasher,
        IPasswordResetEmailSender passwordResetEmailSender,
        IConfiguration configuration)
    {
        _userDatabaseService = userDatabaseService;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _passwordHasher = passwordHasher;
        _passwordResetEmailSender = passwordResetEmailSender;
        _configuration = configuration;
    }


    public async Task<AuthResponse?> SignupAsync(SignupRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var existingUser = await _userDatabaseService.GetUserByEmailAsync(email);

        if (existingUser is not null)
        {
            return null;
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            CurrentTheme = "default",
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        var createdUser = await _userDatabaseService.CreateUserAsync(user);
        return createdUser is null ? null : CreateAuthResponse(createdUser);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _userDatabaseService.GetUserByEmailAsync(email);

        if (user is null)
        {
            return null;
        }

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return CreateAuthResponse(user);
    }

    public async Task RequestPasswordResetAsync(ForgotPasswordRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _userDatabaseService.GetUserByEmailAsync(email);

        if (user is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var rawToken = CreateRawResetToken();
        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashResetToken(rawToken),
            ExpiresAt = now.Add(PasswordResetTokenLifetime),
        };

        await _passwordResetTokenRepository.AddAsync(resetToken);
        await _passwordResetTokenRepository.SaveChangesAsync();

        var resetUrl = BuildResetUrl(rawToken, user.Email);
        await _passwordResetEmailSender.SendAsync(user.Email, resetUrl);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var tokenHash = HashResetToken(request.Token);
        var resetToken = await _passwordResetTokenRepository.Query()
            .SingleOrDefaultAsync(token =>
                token.TokenHash == tokenHash &&
                token.UsedAt == null &&
                token.ExpiresAt > DateTimeOffset.UtcNow);

        if (resetToken is null)
        {
            return false;
        }

        var user = await _userDatabaseService.GetUserByIdAsync(resetToken.UserId);

        if (user is null)
        {
            return false;
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        resetToken.UsedAt = DateTimeOffset.UtcNow;

        await _userDatabaseService.UpdateUserAsync(user);
        _passwordResetTokenRepository.Update(resetToken);
        await _passwordResetTokenRepository.SaveChangesAsync();

        return true;
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(TokenLifetime);
        var token = new JwtSecurityToken(
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
            ],
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(
                GetSigningKey(),
                SecurityAlgorithms.HmacSha256));

        return new AuthResponse(
            user.Id,
            user.Name,
            user.Email,
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }

    private SymmetricSecurityKey GetSigningKey()
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT signing key is not configured.");

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private string BuildResetUrl(string rawToken, string email)
    {
        var frontendUrl = _configuration["Frontend:ResetPasswordUrl"]
            ?? "http://localhost:4200/reset-password";

        return $"{frontendUrl}?token={Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(email)}";
    }

    private static string CreateRawResetToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashResetToken(string rawToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }
}
