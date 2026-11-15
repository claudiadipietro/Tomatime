namespace Tomatime.Api.Services;

public sealed record ProfileInfo(string Name, string Email, int PomoBalance, string CurrentTheme, bool NotificationsEnabled);
public enum PasswordChangeStatus { Success, UserNotFound, IncorrectPassword }

public interface IProfileService
{
    Task<ProfileInfo?> GetAsync(int userId);
    Task<bool> UpdateNotificationsAsync(int userId, bool enabled);
    Task<PasswordChangeStatus> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}
