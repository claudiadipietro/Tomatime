namespace Tomatime.Api.DTOs;

public sealed record DashboardSession(DateTime PlannedStartTime, int Duration);

public sealed record DashboardResponse(
    int PomoBalance,
    DashboardSession? NextSession,
    int CompletedSessions,
    long FocusMinutes);
