namespace Tomatime.Api.DTOs;

public sealed record AuthSessionTokens(
    AuthResponse Response,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
