using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.DTOs;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/push")]
public sealed class PushController : ControllerBase
{
    private readonly PushSubscriptionService _push;

    public PushController(PushSubscriptionService push)
    {
        _push = push;
    }

    private int? UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : null;

    [HttpGet("key")]
    public IActionResult Key() => _push.Configured ? Ok(new { publicKey = _push.PublicKey }) : StatusCode(503, new { message = "Push notifications are not configured." });

    [HttpPut("subscription")]
    public async Task<IActionResult> Subscribe(PushDeviceRequest request)
    {
        if (UserId is not int id)
        {
            return Unauthorized();
        }

        if (!_push.Configured)
        {
            return StatusCode(503, new { message = "Push notifications are not configured." });
        }

        if (!PushSubscriptionService.IsValid(request))
        {
            return BadRequest();
        }

        await _push.SaveAsync(id, request);
        return NoContent();
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe(RemovePushDeviceRequest request)
    {
        if (UserId is not int id)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Endpoint) || request.Endpoint.Length > 2048)
        {
            return BadRequest();
        }

        await _push.RemoveAsync(id, request.Endpoint);
        return NoContent();
    }

    [HttpPost("status")]
    public async Task<IActionResult> Status(RemovePushDeviceRequest request)
    {
        if (UserId is not int id)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Endpoint) || request.Endpoint.Length > 2048)
        {
            return BadRequest();
        }

        return Ok(new { registered = await _push.IsRegisteredAsync(id, request.Endpoint) });
    }
}
