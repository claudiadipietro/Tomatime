using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Services;

namespace Tomatime.Api.Controllers;

public sealed record RewardPurchaseRequest(Guid OperationId);

[ApiController]
[Authorize]
[Route("api/reward-shop")]
public sealed class RewardShopController(IRewardShopService rewards) : ControllerBase
{
    private int? UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (UserId is not int id) return Unauthorized();
        var state = await rewards.GetAsync(id);
        return state is null ? Unauthorized() : Ok(state);
    }

    [HttpPost("{kind}/purchase")]
    public async Task<IActionResult> Purchase(string kind, RewardPurchaseRequest request)
    {
        if (UserId is not int id) return Unauthorized();
        return Map(await rewards.PurchaseAsync(id, kind, request.OperationId));
    }

    [HttpPost("themes/{theme}/activate")]
    public async Task<IActionResult> Activate(string theme)
    {
        if (UserId is not int id) return Unauthorized();
        return Map(await rewards.ActivateAsync(id, theme));
    }

    private IActionResult Map(RewardActionResult result) => result.Status switch
    {
        RewardActionStatus.Success when result.State is not null => Ok(result.State),
        RewardActionStatus.NotFound => Unauthorized(),
        RewardActionStatus.InsufficientBalance => Conflict(new { message = "Insufficient balance." }),
        _ => BadRequest(new { message = "Invalid reward action." }),
    };
}
