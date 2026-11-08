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

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] User user)
    {
        var createdUser = await _userDatabaseService.CreateUserAsync(user);
        if (createdUser == null)
        {
            return BadRequest("Failed to create user.");
        }
        return CreatedAtAction(nameof(GetUserById), new { userId = createdUser.Id }, createdUser);
    }

    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] User user)
    {
        if (userId != user.Id)
        {
            return BadRequest("User ID mismatch.");
        }

        var updatedUser = await _userDatabaseService.UpdateUserAsync(user);
        if (updatedUser == null)
        {
            return NotFound();
        }
        return Ok(updatedUser);
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
}