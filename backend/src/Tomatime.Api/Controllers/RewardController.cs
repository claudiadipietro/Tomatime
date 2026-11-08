using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Models;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Route("api/rewards")]
public class RewardController : ControllerBase
{
    private readonly IRewardDatabaseService _rewardService;
    private readonly IUserRewardDatabaseService _userRewardService;

    public RewardController(IRewardDatabaseService rewardService, IUserRewardDatabaseService userRewardService)
    {
        _rewardService = rewardService;
        _userRewardService = userRewardService;
    }

    [HttpGet("{rewardId}")]
    public async Task<IActionResult> GetRewardById(int rewardId)
    {
        var reward = await _rewardService.GetRewardByIdAsync(rewardId);
        if (reward == null)
        {
            return NotFound();
        }

        return Ok(reward);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReward([FromBody] Reward reward)
    {
        var createdReward = await _rewardService.CreateRewardAsync(reward);
        if (createdReward == null)
        {
            return BadRequest("Failed to create reward.");
        }

        return CreatedAtAction(nameof(GetRewardById), new { rewardId = createdReward.Id }, createdReward);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRewards()
    {
        var rewards = await _rewardService.GetAllRewardsAsync();
        return Ok(rewards);
    }

    [HttpPost("{rewardId}/redeem/{userId}")]
    public async Task<IActionResult> RedeemReward(int rewardId, int userId)
    {
        var userReward = new UserReward
        {
            UserId = userId,
            RewardId = rewardId
        };

        var createdUserReward = await _userRewardService.CreateUserRewardAsync(userReward);
        if (createdUserReward == null)
        {
            return BadRequest("Failed to redeem reward.");
        }

        return Ok(createdUserReward);
    }
}