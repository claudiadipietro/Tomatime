namespace Tomatime.Api.Models;

public sealed class ReminderDelivery
{
    /// <summary>
    /// Gets or sets the unique identifier for the session.
    /// </summary>
    public int SessionId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the push device.
    /// </summary>
    public int PushDeviceId { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the reminder was sent.
    /// </summary>
    public DateTime SentAt { get; set; }
}
