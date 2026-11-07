using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public interface IRewardDatabaseService
{
    Task<Reward?> GetRewardByIdAsync(int rewardId);
    Task<Reward?> CreateRewardAsync(Reward reward);
    Task<Reward?> UpdateRewardAsync(Reward reward);
    Task<bool> DeleteRewardAsync(int rewardId);
    Task<List<Reward>> GetAllRewardsAsync();
}