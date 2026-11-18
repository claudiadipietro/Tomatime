using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Tomatime.Api.Context;
using Tomatime.Api.DTOs;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class AuthService: IAuthService
{
    private readonly IUserDatabaseService _userDatabaseService;
    private readonly IRepository<PasswordResetToken> _passwordResetTokenRepository;
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly PasswordResetEmailQueue _passwordResetEmailQueue;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AuthService> _logger;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromMinutes(30);

    public AuthService(
        IUserDatabaseService userDatabaseService,
        IRepository<PasswordResetToken> passwordResetTokenRepository,
        IRepository<RefreshToken> refreshTokenRepository,
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        PasswordResetEmailQueue passwordResetEmailQueue,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<AuthService> logger)
    {
        _userDatabaseService = userDatabaseService;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _db = db;
        _passwordHasher = passwordHasher;
        _passwordResetEmailQueue = passwordResetEmailQueue;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }


    public async Task<AuthSessionTokens?> SignupAsync(SignupRequest request)
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
        return createdUser is null ? null : await CreateAuthSessionAsync(createdUser);
    }

    public async Task<AuthSessionTokens?> LoginAsync(LoginRequest request)
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

        return await CreateAuthSessionAsync(user);
    }

    public async Task<AuthSessionTokens?> RefreshAsync(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        string tokenHash = HashResetToken(refreshToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        RefreshToken? existingToken = await _refreshTokenRepository.Query()
            .SingleOrDefaultAsync(token =>
                token.TokenHash == tokenHash &&
                token.RevokedAt == null &&
                token.ExpiresAt > now);

        if (existingToken is null)
        {
            return null;
        }

        int revoked = await _db.RefreshTokens
            .Where(token => token.Id == existingToken.Id && token.RevokedAt == null && token.ExpiresAt > now)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.RevokedAt, now));

        if (revoked != 1)
        {
            return null;
        }

        User? user = await _userDatabaseService.GetUserByIdAsync(existingToken.UserId);
        if (user is null)
        {
            return null;
        }

        AuthSessionTokens newSession = await CreateAuthSessionAsync(user);
        await transaction.CommitAsync();
        return newSession;
    }

    public async Task RevokeRefreshTokenAsync(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        string tokenHash = HashResetToken(refreshToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await _db.RefreshTokens
            .Where(token => token.TokenHash == tokenHash && token.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.RevokedAt, now));
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
        await _db.RefreshTokens
            .Where(token => token.UserId == user.Id && token.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.RevokedAt, DateTimeOffset.UtcNow));
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

    private async Task<AuthSessionTokens> CreateAuthSessionAsync(User user)
    {
        string refreshToken = CreateRawResetToken();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset refreshTokenExpiresAt = now.Add(RefreshTokenLifetime);

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashResetToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = refreshTokenExpiresAt,
        });
        await _refreshTokenRepository.SaveChangesAsync();

        return new AuthSessionTokens(CreateAuthResponse(user), refreshToken, refreshTokenExpiresAt);
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
