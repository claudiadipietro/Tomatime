namespace Tomatime.Api.Models;

public sealed class ReminderDelivery
{
    public int SessionId { get; set; }
    public int PushDeviceId { get; set; }
    public DateTime SentAt { get; set; }
}
