using Microsoft.EntityFrameworkCore;
using Tomatime.Api.Context;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class RewardShopService(AppDbContext db) : IRewardShopService
{
    internal static async Task<RewardShopState> StateAsync(AppDbContext db, User user)
    {
        var owned = await db.UserRewards.Where(r => r.UserId == user.Id)
            .Select(r => r.RewardId).ToListAsync();
        return new(user.PomoBalance,
            await db.UserRewards.CountAsync(o => o.UserId == user.Id && o.RewardId == RewardCatalog.ExtraBreak && o.Kind == "extra-break" && o.SessionId == null),
            user.CurrentTheme, owned.Contains(RewardCatalog.Pink), owned.Contains(RewardCatalog.Blue));
    }

    public async Task<RewardShopState?> GetAsync(int userId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE").SingleOrDefaultAsync();
        if (user is null) return null;
        var state = await StateAsync(db, user);
        await tx.CommitAsync();
        return state;
    }

    public async Task<RewardActionResult> PurchaseAsync(int userId, string kind, Guid operationId)
    {
        if (operationId == Guid.Empty || kind is not ("pink" or "blue" or "extra-break"))
            return new(RewardActionStatus.InvalidOperation);
        var rewardId = kind switch { "pink" => RewardCatalog.Pink, "blue" => RewardCatalog.Blue, _ => RewardCatalog.ExtraBreak };
        await using var tx = await db.Database.BeginTransactionAsync();
        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE").SingleOrDefaultAsync();
        if (user is null) return new(RewardActionStatus.NotFound);
        var previous = await db.UserRewards.AsNoTracking().SingleOrDefaultAsync(o => o.OperationId == operationId);
        if (previous is not null)
            return previous.UserId == userId && previous.RewardId == rewardId && previous.Kind == kind
                ? new(RewardActionStatus.Success, await StateAsync(db, user)) : new(RewardActionStatus.InvalidOperation);
        if (rewardId != RewardCatalog.ExtraBreak &&
            await db.UserRewards.AnyAsync(r => r.UserId == userId && r.RewardId == rewardId))
            return new(RewardActionStatus.InvalidOperation);
        var reward = await db.Rewards.SingleAsync(r => r.Id == rewardId);
        if (user.PomoBalance < reward.Cost) return new(RewardActionStatus.InsufficientBalance);
        user.PomoBalance -= reward.Cost;
        db.UserRewards.Add(new UserReward { OperationId = operationId, UserId = userId, RewardId = rewardId, Kind = kind, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var state = await StateAsync(db, user);
        await tx.CommitAsync();
        return new(RewardActionStatus.Success, state);
    }

    public async Task<RewardActionResult> ActivateAsync(int userId, string theme)
    {
        if (theme is not ("default" or "pink" or "blue")) return new(RewardActionStatus.InvalidOperation);
        await using var tx = await db.Database.BeginTransactionAsync();
        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE").SingleOrDefaultAsync();
        if (user is null) return new(RewardActionStatus.NotFound);
        var rewardId = theme == "pink" ? RewardCatalog.Pink : RewardCatalog.Blue;
        if (theme != "default" && !await db.UserRewards.AnyAsync(r => r.UserId == userId && r.RewardId == rewardId))
            return new(RewardActionStatus.InvalidOperation);
        user.CurrentTheme = theme;
        await db.SaveChangesAsync();
        var state = await StateAsync(db, user);
        await tx.CommitAsync();
        return new(RewardActionStatus.Success, state);
    }
}
