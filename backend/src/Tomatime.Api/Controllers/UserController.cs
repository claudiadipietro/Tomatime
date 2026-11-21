using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Tomatime.Api.DTOs;
using Tomatime.Api.Models;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserRewardDatabaseService _userRewardDatabaseService;
    private readonly IProfileService _profileService;
    private readonly IRewardShopService _rewardShopService;

    public UserController(IUserRewardDatabaseService userRewardDatabaseService, IProfileService profileService, IRewardShopService rewardShopService)
    {
        _userRewardDatabaseService = userRewardDatabaseService;
        _profileService = profileService;
        _rewardShopService = rewardShopService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Get()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        ProfileInfo? result = await _profileService.GetAsync(userId);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPut("notifications")]
    public async Task<IActionResult> UpdateNotifications(NotificationPreferenceRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        if (request.Enabled is not bool enabled)
        {
            return BadRequest();
        }

        return await _profileService.UpdateNotificationsAsync(userId, enabled) ? NoContent() : Unauthorized();
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        ProfileInfo? profile = await _profileService.GetAsync(userId);
        return profile is null ? Unauthorized() : Ok(profile.NotificationsEnabled);
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        return await _profileService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword) switch
        {
            PasswordChangeStatus.Success => NoContent(),
            PasswordChangeStatus.IncorrectPassword => BadRequest(new { message = "Incorrect current password." }),
            _ => Unauthorized(),
        };
    }

    [HttpGet("rewards")]
    public async Task<IActionResult> GetRewardState()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        RewardShopState? state = await _rewardShopService.GetAsync(userId);
        return state is null ? Unauthorized() : Ok(state);
    }

    [HttpPut("theme")]
    public async Task<IActionResult> UpdateTheme(UpdateThemeRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return Unauthorized();
        }

        RewardActionResult result = await _rewardShopService.ActivateAsync(userId, request.Theme);
        return result.Status switch
        {
            RewardActionStatus.Success when result.State is not null => Ok(result.State),
            RewardActionStatus.NotFound => Unauthorized(),
            RewardActionStatus.InsufficientBalance => Conflict(new { message = "Insufficient balance." }),
            _ => BadRequest(new { message = "Invalid reward action." }),
        };
    }

    [HttpGet("profile/{userId}")]
    public async Task<IActionResult> GetUserById(int userId)
    {
        if (!IsCurrentUser(userId))
        {
            return Unauthorized();
        }

        ProfileInfo? user = await _profileService.GetAsync(userId);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    [HttpGet("{userId}/rewards")]
    public async Task<IActionResult> GetUserRewards(int userId)
    {
        if (!IsCurrentUser(userId))
        {
            return Unauthorized();
        }

        List<UserReward> userRewards = await _userRewardDatabaseService.GetUserRewardsByUserIdAsync(userId);
        return Ok(userRewards);
    }

    [HttpGet("notifications/{userId}")]
    public async Task<IActionResult> GetUserNotifications(int userId)
    {
        if (!IsCurrentUser(userId))
        {
            return Unauthorized();
        }

        ProfileInfo? user = await _profileService.GetAsync(userId);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user.NotificationsEnabled);
    }

    [HttpPut("notifications/{userId}")]
    public async Task<IActionResult> UpdateUserNotifications(int userId, [FromBody] bool notificationsEnabled)
    {
        if (!IsCurrentUser(userId))
        {
            return Unauthorized();
        }

        return await _profileService.UpdateNotificationsAsync(userId, notificationsEnabled)
            ? Ok(notificationsEnabled) : NotFound();
    }

    private bool IsCurrentUser(int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) && id == userId;
}