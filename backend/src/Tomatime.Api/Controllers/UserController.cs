using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Tomatime.Api.Models;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserDatabaseService _userDatabaseService;
    private readonly IUserRewardDatabaseService _userRewardDatabaseService;
    private readonly IProfileService _profileService;

    public UserController(IUserDatabaseService userDatabaseService, IUserRewardDatabaseService userRewardDatabaseService, IProfileService profileService)
    {
        _userDatabaseService = userDatabaseService;
        _userRewardDatabaseService = userRewardDatabaseService;
        _profileService = profileService;
    }

    [HttpGet("profile/{userId}")]
    public async Task<IActionResult> GetUserById(int userId)
    {
        var user = await _userDatabaseService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        var user = await _userDatabaseService.GetUserByEmailAsync(email);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        var success = await _userDatabaseService.DeleteUserAsync(userId);
        if (!success)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpGet("{userId}/rewards")]
    public async Task<IActionResult> GetUserRewards(int userId)
    {
        var userRewards = await _userRewardDatabaseService.GetUserRewardsByUserIdAsync(userId);
        return Ok(userRewards);
    }

    [HttpGet("notifications/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetUserNotifications(int userId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) || id != userId) return Unauthorized();
        var user = await _profileService.GetAsync(userId);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user.NotificationsEnabled);
    }

    [HttpPut("notifications/{userId}")]
    [Authorize]
    public async Task<IActionResult> UpdateUserNotifications(int userId, [FromBody] bool notificationsEnabled)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) || id != userId) return Unauthorized();
        return await _profileService.UpdateNotificationsAsync(userId, notificationsEnabled)
            ? Ok(notificationsEnabled) : NotFound();
    }
}