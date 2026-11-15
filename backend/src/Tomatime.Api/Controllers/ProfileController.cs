using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

public sealed record NotificationPreferenceRequest([param: Required] bool? Enabled);
public sealed record ChangePasswordRequest(
    [param: Required, StringLength(128)] string CurrentPassword,
    [param: Required, MinLength(8), StringLength(128), RegularExpression(@".*\S.*")] string NewPassword);

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(IProfileService profile) : ControllerBase
{
    private int? UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (UserId is not int id) return Unauthorized();
        var result = await profile.GetAsync(id);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPut("notifications")]
    public async Task<IActionResult> UpdateNotifications(NotificationPreferenceRequest request)
    {
        if (UserId is not int id) return Unauthorized();
        if (request.Enabled is not bool enabled) return BadRequest();
        return await profile.UpdateNotificationsAsync(id, enabled) ? NoContent() : Unauthorized();
    }

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (UserId is not int id) return Unauthorized();
        return await profile.ChangePasswordAsync(id, request.CurrentPassword, request.NewPassword) switch
        {
            PasswordChangeStatus.Success => NoContent(),
            PasswordChangeStatus.IncorrectPassword => BadRequest(new { message = "Incorrect current password." }),
            _ => Unauthorized(),
        };
    }
}
