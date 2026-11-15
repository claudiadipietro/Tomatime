namespace Tomatime.Api.Models;

public class RewardOperation
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public int RewardId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public int? SessionId { get; set; }
    public DateTime CreatedAt { get; set; }
}
