namespace Tomatime.Api.DTOs;

public sealed record DashboardResponse(
    int PomoBalance,
    DashboardSession? NextSession,
    int CompletedSessions,
    long FocusMinutes,
    IReadOnlyList<DashboardActiveSession> ActiveSessions);
