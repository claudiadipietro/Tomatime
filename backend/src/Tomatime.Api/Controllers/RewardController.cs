using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Models;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Route("api/rewards")]
public class RewardController : ControllerBase
{
    private readonly IRewardDatabaseService _rewardService;

    public RewardController(IRewardDatabaseService rewardService)
    {
        _rewardService = rewardService;
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
}