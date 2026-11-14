namespace Tomatime.Api.DTOs;

public sealed record StartedSessionResponse(int Id, int Duration, DateTime? ActualStartTime);
