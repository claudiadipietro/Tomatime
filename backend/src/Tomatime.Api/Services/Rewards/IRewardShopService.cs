namespace Tomatime.Api.Services;

public sealed record RewardShopState(int PomoBalance, int PendingExtraBreaks, string CurrentTheme, bool PinkUnlocked, bool BlueUnlocked);
public enum RewardActionStatus { Success, NotFound, InsufficientBalance, InvalidOperation }
public sealed record RewardActionResult(RewardActionStatus Status, RewardShopState? State = null);

public interface IRewardShopService
{
    Task<RewardShopState?> GetAsync(int userId);
    Task<RewardActionResult> PurchaseAsync(int userId, string kind, Guid operationId);
    Task<RewardActionResult> ActivateAsync(int userId, string theme);
}
