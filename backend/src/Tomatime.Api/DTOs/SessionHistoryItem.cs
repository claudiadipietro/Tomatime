namespace Tomatime.Api.DTOs;

public sealed record SessionHistoryItem(
    int Id, int Duration, string Status, DateTime? ActualStartTime,
    DateTime? EndedAt, int PomosEarned, DateTime PlannedStartTime);
