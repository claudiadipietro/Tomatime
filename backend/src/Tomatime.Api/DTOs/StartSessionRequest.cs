using System.ComponentModel.DataAnnotations;

namespace Tomatime.Api.DTOs;

public sealed class StartSessionRequest
{
    [Range(25, 300)]
    public int Duration { get; init; }
}
