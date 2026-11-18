using Tomatime.Api.Models;

namespace Tomatime.Api.DTOs;

public sealed record SessionPlanningResult(
    SessionPlanningStatus Status,
    PlannedSessionResponse? Session = null);
