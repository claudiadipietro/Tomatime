using Tomatime.Api.DTOs;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public interface IProfileService
{
    Task<ProfileInfo?> GetAsync(int userId);
    Task<bool> UpdateNotificationsAsync(int userId, bool enabled);
    Task<PasswordChangeStatus> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}
