using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.DTOs;
using Tomatime.Api.Models;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Route("api/rewards")]
public class RewardController : ControllerBase
{
    private readonly IRewardDatabaseService _rewardService;
    private readonly IRewardShopService _shop;
    private readonly IThemeDatabaseService _themes;

    public RewardController(IRewardDatabaseService rewardService, IRewardShopService shop, IThemeDatabaseService themes)
    {
        _rewardService = rewardService;
        _shop = shop;
        _themes = themes;
    }

    [HttpGet("{rewardId:int}")]
    public async Task<IActionResult> GetRewardById(int rewardId)
    {
        Reward? reward = await _rewardService.GetRewardByIdAsync(rewardId);
        if (reward == null)
        {
            return NotFound();
        }

        return Ok(reward);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRewards()
    {
        List<Reward> rewards = await _rewardService.GetAllRewardsAsync();
        return Ok(rewards);
    }

    [HttpGet("/api/themes")]
    public async Task<IActionResult> GetThemes() => Ok(await _themes.GetAllThemesAsync());

    [HttpGet("/api/themes/{themeId:int}")]
    public async Task<IActionResult> GetTheme(int themeId)
    {
        Theme? theme = await _themes.GetThemeByIdAsync(themeId);
        return theme is null ? NotFound() : Ok(theme);
    }

    [Authorize]
    [HttpPost("{rewardId:int}/redeem")]
    public Task<IActionResult> Redeem(int rewardId, RewardPurchaseRequest request)
    {
        string? kind = rewardId switch
        {
            RewardCatalog.Pink => "pink",
            RewardCatalog.Blue => "blue",
            RewardCatalog.ExtraBreak => "extra-break",
            _ => null
        };
        return kind is null ? Task.FromResult<IActionResult>(BadRequest(new { message = "Invalid reward action." })) : Purchase(kind, request);
    }

    private async Task<IActionResult> Purchase(string kind, RewardPurchaseRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        return Map(await _shop.PurchaseAsync(userId, kind, request.OperationId));
    }

    private IActionResult Map(RewardActionResult result) => result.Status switch
    {
        RewardActionStatus.Success when result.State is not null => Ok(result.State),
        RewardActionStatus.NotFound => Unauthorized(),
        RewardActionStatus.InsufficientBalance => Conflict(new { message = "Insufficient balance." }),
        _ => BadRequest(new { message = "Invalid reward action." }),
    };
}