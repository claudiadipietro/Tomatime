namespace Tomatime.Api.Models;

/// <summary>
/// Represents a reward in the application.
/// </summary>
public class Reward
{
    /// <summary>
    /// Gets or sets the unique identifier for the reward.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the reward.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the reward.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cost of the reward in pomos.
    /// </summary>
    public int Cost { get; set; }

    /// <summary>
    /// Gets or sets the type of the reward.
    /// </summary>
    public RewardType Type { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the theme associated with the reward, if applicable.
    /// </summary>
    public int? ThemeId { get; set; }

    /// <summary>
    /// Gets or sets the theme associated with the reward, if applicable.
    /// </summary>
    public Theme? Theme { get; set; }

}