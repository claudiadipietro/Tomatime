namespace Tomatime.Api.Models;

/// <summary>
/// Represents a user in the application.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
	public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the user.
    /// </summary>
	public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the user.
    /// </summary>
	public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed password of the user.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Pomo balance of the user.
    /// </summary>
    public int PomoBalance { get; set; } = 0;

    /// <summary>
    /// Gets or sets the current theme of the user.
    /// </summary>
    public string CurrentTheme { get; set; } = "default";

    /// <summary>
    /// Gets or sets a value indicating whether notifications are enabled for the user.
    /// </summary>
    public bool NotificationsEnabled { get; set; } = true;
}
