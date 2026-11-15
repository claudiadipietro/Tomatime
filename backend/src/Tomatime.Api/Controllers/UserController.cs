using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Models;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserDatabaseService _userDatabaseService;
    private readonly IUserRewardDatabaseService _userRewardDatabaseService;

    public UserController(IUserDatabaseService userDatabaseService, IUserRewardDatabaseService userRewardDatabaseService)
    {
        _userDatabaseService = userDatabaseService;
        _userRewardDatabaseService = userRewardDatabaseService;
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
    public async Task<IActionResult> GetUserNotifications(int userId)
    {
        var user = await _userDatabaseService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user.NotificationsEnabled);
    }

    [HttpPut("notifications/{userId}")]
    public async Task<IActionResult> UpdateUserNotifications(int userId, [FromBody] bool notificationsEnabled)
    {
        var user = await _userDatabaseService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        user.NotificationsEnabled = notificationsEnabled;
        var updatedUser = await _userDatabaseService.UpdateUserAsync(user);
        if (updatedUser == null)
        {
            return BadRequest("Failed to update user notifications.");
        }

        return Ok(updatedUser.NotificationsEnabled);
    }
}