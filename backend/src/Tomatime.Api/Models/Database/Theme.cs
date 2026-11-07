namespace Tomatime.Api.Models;

/// <summary>
/// Represents a theme in the application.
/// </summary>
public class Theme
{
    /// <summary>
    /// Gets or sets the unique identifier for the theme.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the theme.
    /// </summary>
    public string Name { get; set; } = string.Empty;

}