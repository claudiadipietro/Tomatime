namespace Tomatime.Api.DTOs;

public sealed record CalendarSession(
    int Id, int Duration, string Status, DateTime PlannedStartTime,
    DateTime? ActualStartTime, int PomosEarned);
