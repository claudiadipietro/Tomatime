namespace Tomatime.Api.Models;

/// <summary>
/// Represents a session in the application.
/// </summary>
public class Session
{
    /// <summary>
    /// Gets or sets the unique identifier for the session.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID associated with the session.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the planned start time of the session.
    /// </summary>
    public DateTime PlannedStartTime { get; set; }

    /// <summary>
    /// Gets or sets the duration of the session.
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Gets or sets the status of the session.
    /// </summary>
    public SessionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the number of pomos earned during the session.
    /// </summary>
    public int PomosEarned { get; set; }

    /// <summary>
    /// Gets or sets the user associated with the session.
    /// </summary>
    public User User { get; set; } = null!;
}