using Tomatime.Api.DTOs;

namespace Tomatime.Api.Services;

public interface IRewardShopService
{
    Task<RewardShopState?> GetAsync(int userId);
    Task<RewardActionResult> PurchaseAsync(int userId, string kind, Guid operationId);
    Task<RewardActionResult> ActivateAsync(int userId, string theme);
}
