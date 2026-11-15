using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

public sealed record RemovePushDeviceRequest(string Endpoint);

[ApiController]
[Authorize]
[Route("api/push")]
public sealed class PushController(PushSubscriptionService push) : ControllerBase
{
    private int? UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet("key")]
    public IActionResult Key() => push.Configured ? Ok(new { publicKey = push.PublicKey }) : StatusCode(503, new { message = "Push notifications are not configured." });

    [HttpPut("subscription")]
    public async Task<IActionResult> Subscribe(PushDeviceRequest request)
    {
        if (UserId is not int id) return Unauthorized();
        if (!push.Configured) return StatusCode(503, new { message = "Push notifications are not configured." });
        if (!PushSubscriptionService.IsValid(request)) return BadRequest();
        await push.SaveAsync(id, request);
        return NoContent();
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe(RemovePushDeviceRequest request)
    {
        if (UserId is not int id) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Endpoint) || request.Endpoint.Length > 2048) return BadRequest();
        await push.RemoveAsync(id, request.Endpoint);
        return NoContent();
    }

    [HttpPost("status")]
    public async Task<IActionResult> Status(RemovePushDeviceRequest request)
    {
        if (UserId is not int id) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Endpoint) || request.Endpoint.Length > 2048) return BadRequest();
        return Ok(new { registered = await push.IsRegisteredAsync(id, request.Endpoint) });
    }
}
