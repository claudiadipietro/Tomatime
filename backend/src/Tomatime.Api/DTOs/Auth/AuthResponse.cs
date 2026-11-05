namespace Tomatime.Api.DTOs;

public sealed record AuthResponse(
    int UserId,
    string Name,
    string Email,
    string AccessToken,
    DateTimeOffset ExpiresAt);
