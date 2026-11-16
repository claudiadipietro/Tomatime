namespace Tomatime.Api.DTOs;

public sealed record DashboardSession(int Id, DateTime PlannedStartTime, int Duration);
public sealed record DashboardActiveSession(int Id, DateTime ActualStartTime, int Duration);

public sealed record DashboardResponse(
    int PomoBalance,
    DashboardSession? NextSession,
    int CompletedSessions,
    long FocusMinutes,
    IReadOnlyList<DashboardActiveSession> ActiveSessions);
