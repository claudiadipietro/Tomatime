using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public interface IUserDatabaseService
{
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> CreateUserAsync(User user);
    Task<User?> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(int userId);
}