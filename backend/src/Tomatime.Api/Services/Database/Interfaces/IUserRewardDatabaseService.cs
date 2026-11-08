using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public interface IUserRewardDatabaseService
{
    Task<UserReward?> GetUserRewardByIdAsync(int userRewardId);
    Task<UserReward?> CreateUserRewardAsync(UserReward userReward);
    Task<UserReward?> UpdateUserRewardAsync(UserReward userReward);
    Task<bool> DeleteUserRewardAsync(int userRewardId);
    Task<List<UserReward>> GetUserRewardsByUserIdAsync(int userId);
}