using System.ComponentModel.DataAnnotations;

namespace Tomatime.Api.DTOs;

public sealed class PlanSessionRequest
{
    [Range(25, 300)]
    public int Duration { get; init; }

    [Required]
    public DateTimeOffset? PlannedStartTime { get; init; }
}
