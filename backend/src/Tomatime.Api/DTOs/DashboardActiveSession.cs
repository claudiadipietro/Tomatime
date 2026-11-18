namespace Tomatime.Api.DTOs;

public sealed record DashboardActiveSession(int Id, DateTime ActualStartTime, int Duration);
