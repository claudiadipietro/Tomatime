using Tomatime.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Tomatime.Api.Services;

public class UserRewardDatabaseService : IUserRewardDatabaseService
{
    private readonly IRepository<UserReward> _repository;

    public UserRewardDatabaseService(IRepository<UserReward> repository)
    {
        _repository = repository;
    }

    public async Task<UserReward?> GetUserRewardByIdAsync(int userRewardId)
    {
        return await _repository.GetByIdAsync(userRewardId);
    }

    public async Task<UserReward?> CreateUserRewardAsync(UserReward userReward)
    {
        await _repository.AddAsync(userReward);
        await _repository.SaveChangesAsync();
        return userReward;
    }

    public async Task<UserReward?> UpdateUserRewardAsync(UserReward userReward)
    {
        _repository.Update(userReward);
        await _repository.SaveChangesAsync();
        return userReward;
    }

    public async Task<bool> DeleteUserRewardAsync(int userRewardId)
    {
        UserReward? userReward = await _repository.GetByIdAsync(userRewardId);
        if (userReward == null)
        {
            return false;
        }

        _repository.Remove(userReward);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserReward>> GetUserRewardsByUserIdAsync(int userId)
    {
        return await _repository.Query()
            .Where(ur => ur.UserId == userId)
            .ToListAsync();
    }
}