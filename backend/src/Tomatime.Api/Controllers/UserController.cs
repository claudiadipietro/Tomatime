using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserRewardDatabaseService _userRewardDatabaseService;
    private readonly IProfileService _profileService;

    public UserController(IUserRewardDatabaseService userRewardDatabaseService, IProfileService profileService)
    {
        _userRewardDatabaseService = userRewardDatabaseService;
        _profileService = profileService;
    }

    [HttpGet("profile/{userId}")]
    public async Task<IActionResult> GetUserById(int userId)
    {
        if (!IsCurrentUser(userId)) return Unauthorized();
        var user = await _profileService.GetAsync(userId);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    [HttpGet("{userId}/rewards")]
    public async Task<IActionResult> GetUserRewards(int userId)
    {
        if (!IsCurrentUser(userId)) return Unauthorized();
        var userRewards = await _userRewardDatabaseService.GetUserRewardsByUserIdAsync(userId);
        return Ok(userRewards);
    }

    [HttpGet("notifications/{userId}")]
    public async Task<IActionResult> GetUserNotifications(int userId)
    {
        if (!IsCurrentUser(userId)) return Unauthorized();
        var user = await _profileService.GetAsync(userId);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user.NotificationsEnabled);
    }

    [HttpPut("notifications/{userId}")]
    public async Task<IActionResult> UpdateUserNotifications(int userId, [FromBody] bool notificationsEnabled)
    {
        if (!IsCurrentUser(userId)) return Unauthorized();
        return await _profileService.UpdateNotificationsAsync(userId, notificationsEnabled)
            ? Ok(notificationsEnabled) : NotFound();
    }

    private bool IsCurrentUser(int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id == userId;
}