using Microsoft.EntityFrameworkCore;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public class RewardDatabaseService : IRewardDatabaseService
{
    private readonly IRepository<Reward> _repository;

    public RewardDatabaseService(IRepository<Reward> repository)
    {
        _repository = repository;
    }

    public async Task<Reward?> CreateRewardAsync(Reward reward)
    {
        Reward? createdReward = await GetRewardByIdAsync(reward.Id);
        if (createdReward != null)
        {
            return null;
        }

        await _repository.AddAsync(reward);
        await _repository.SaveChangesAsync();
        return reward;
    }

    public async Task<bool> DeleteRewardAsync(int rewardId)
    {
        Reward? reward = await GetRewardByIdAsync(rewardId);
        if (reward == null)
        {
            return false;
        }

        _repository.Remove(reward);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<Reward?> GetRewardByIdAsync(int rewardId)
    {
        return await _repository.GetByIdAsync(rewardId);
    }

    public async Task<Reward?> UpdateRewardAsync(Reward reward)
    {
        Reward? existingReward = await GetRewardByIdAsync(reward.Id);

        if (existingReward == null)
        {
            return null;
        }

        existingReward.Name = reward.Name;
        existingReward.Description = reward.Description;
        existingReward.Cost = reward.Cost;

        _repository.Update(reward);
        await _repository.SaveChangesAsync();
        return reward;
    }

    public async Task<List<Reward>> GetAllRewardsAsync()
    {
        return await _repository.Query().ToListAsync();
    }
}