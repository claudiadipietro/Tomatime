using Tomatime.Api.Models;

namespace Tomatime.Api.DTOs;

public sealed record RewardActionResult(RewardActionStatus Status, RewardShopState? State = null);
