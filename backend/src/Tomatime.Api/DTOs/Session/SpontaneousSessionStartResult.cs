using Tomatime.Api.Models;

namespace Tomatime.Api.DTOs;

public sealed record SpontaneousSessionStartResult(
    SpontaneousSessionStartStatus Status,
    StartedSessionResponse? Session = null);
