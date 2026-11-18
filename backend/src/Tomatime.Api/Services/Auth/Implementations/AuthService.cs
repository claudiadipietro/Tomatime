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
    private readonly PasswordResetEmailQueue _passwordResetEmailQueue;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AuthService> _logger;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromMinutes(30);

    public AuthService(
        IUserDatabaseService userDatabaseService,
        IRepository<PasswordResetToken> passwordResetTokenRepository,
        IPasswordHasher<User> passwordHasher,
        PasswordResetEmailQueue passwordResetEmailQueue,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<AuthService> logger)
    {
        _userDatabaseService = userDatabaseService;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _passwordHasher = passwordHasher;
        _passwordResetEmailQueue = passwordResetEmailQueue;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }


    public async Task<AuthResponse?> SignupAsync(SignupRequest request)
    {
        string email = NormalizeEmail(request.Email);
        User? existingUser = await _userDatabaseService.GetUserByEmailAsync(email);

        if (existingUser is not null)
        {
            return null;
        }

        User user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            CurrentTheme = "default"
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        User? createdUser = await _userDatabaseService.CreateUserAsync(user);
        return createdUser is null ? null : CreateAuthResponse(createdUser);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        string email = NormalizeEmail(request.Email);
        User? user = await _userDatabaseService.GetUserByEmailAsync(email);

        if (user is null)
        {
            return null;
        }

        PasswordVerificationResult verification = _passwordHasher.VerifyHashedPassword(
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
        string frontendUrl = GetResetPasswordUrl();
        string email = NormalizeEmail(request.Email);
        User? user = await _userDatabaseService.GetUserByEmailAsync(email);

        if (user is null)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        string rawToken = CreateRawResetToken();
        PasswordResetToken resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashResetToken(rawToken),
            ExpiresAt = now.Add(PasswordResetTokenLifetime),
        };

        await _passwordResetTokenRepository.AddAsync(resetToken);
        await _passwordResetTokenRepository.SaveChangesAsync();

        string resetUrl = $"{frontendUrl}?token={Uri.EscapeDataString(rawToken)}";
        if (!_passwordResetEmailQueue.TryEnqueue(new PasswordResetEmail(user.Email, resetUrl)))
        {
            _logger.LogError("Password reset email could not be queued because the queue is full.");
        }
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        string tokenHash = HashResetToken(request.Token);
        PasswordResetToken? resetToken = await _passwordResetTokenRepository.Query()
            .SingleOrDefaultAsync(token =>
                token.TokenHash == tokenHash &&
                token.UsedAt == null &&
                token.ExpiresAt > DateTimeOffset.UtcNow);

        if (resetToken is null)
        {
            return false;
        }

        User? user = await _userDatabaseService.GetUserByIdAsync(resetToken.UserId);

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
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset expiresAt = now.Add(TokenLifetime);
        JwtSecurityToken token = new JwtSecurityToken(
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
        string key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT signing key is not configured.");

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private string GetResetPasswordUrl()
    {
        string? frontendUrl = _configuration["Frontend:ResetPasswordUrl"];
        if (string.IsNullOrWhiteSpace(frontendUrl) && _environment.IsDevelopment())
        {
            frontendUrl = "http://localhost:4200/reset-password";
        }

        if (!Uri.TryCreate(frontendUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps &&
                !(_environment.IsDevelopment() && uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp)) ||
            (!_environment.IsDevelopment() && uri.IsLoopback) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException("Frontend:ResetPasswordUrl must be an absolute HTTPS URL without credentials, query or fragment; HTTP localhost is allowed only in development.");
        }

        return uri.AbsoluteUri;
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
