namespace Tomatime.Api.Models;

public sealed class PasswordResetToken
{
    /// <summary>
    /// Gets or sets the unique identifier for the password reset token.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the user associated with the password reset token.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the hashed value of the password reset token.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the password reset token expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the password reset token was used, if applicable.
    /// </summary>
    public DateTimeOffset? UsedAt { get; set; }

}
