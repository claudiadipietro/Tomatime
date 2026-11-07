namespace Tomatime.Api.Models;

/// <summary>
/// Represents a user reward in the application.
/// </summary>
public class UserReward
{
    /// <summary>
    /// Gets or sets the unique identifier for the user reward.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the reward.
    /// </summary>
    public int RewardId { get; set; }

}