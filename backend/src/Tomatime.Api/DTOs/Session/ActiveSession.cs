namespace Tomatime.Api.DTOs;

public sealed record ActiveSession(int Id, int Duration, DateTime ActualStartTime, DateTime ServerTime, int[] BreakExtensions, int PomoBalance, int ExtraBreakCost);
