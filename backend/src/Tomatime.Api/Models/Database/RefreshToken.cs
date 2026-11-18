namespace Tomatime.Api.Models;

public sealed class RefreshToken
{
    /// <summary>
    /// The unique identifier of the refresh token.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The identifier of the user to whom the refresh token belongs.
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// The hashed value of the refresh token.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// The date and time when the refresh token was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The date and time when the refresh token expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
    
    /// <summary>
    /// The date and time when the refresh token was revoked, if applicable.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }
}
