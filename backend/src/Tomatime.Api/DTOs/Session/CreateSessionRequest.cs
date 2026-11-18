using System.ComponentModel.DataAnnotations;

namespace Tomatime.Api.DTOs;

public sealed class CreateSessionRequest
{
    [Range(25, 300)]
    public int Duration { get; init; }

    public DateTimeOffset? PlannedStartTime { get; init; }
}
