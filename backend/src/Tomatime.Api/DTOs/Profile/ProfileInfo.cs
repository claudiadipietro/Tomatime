namespace Tomatime.Api.DTOs;

public sealed record ProfileInfo(string Name, string Email, int PomoBalance, string CurrentTheme, bool NotificationsEnabled);
