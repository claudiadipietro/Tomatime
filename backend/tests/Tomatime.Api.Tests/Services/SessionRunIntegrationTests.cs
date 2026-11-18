using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tomatime.Api.Context;
using Tomatime.Api.Models;
using Tomatime.Api.Services;

namespace Tomatime.Api.Tests;

[Collection("Database integration")]
public class SessionRunIntegrationTests
{
    [PushDatabaseFact]
    public async Task RepeatedRewardOperationChargesTheCatalogPriceOnlyOnce()
    {
        var connection = Environment.GetEnvironmentVariable("TOMATIME_PUSH_TEST_CONNECTION")!;
        var parsed = new NpgsqlConnectionStringBuilder(connection);
        Assert.True(parsed.Host is "localhost" or "127.0.0.1" &&
                    parsed.Database!.EndsWith("_test", StringComparison.Ordinal));
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
        var cost = await db.Rewards.Where(reward => reward.Id == RewardCatalog.Pink).Select(reward => reward.Cost).SingleAsync();
        var user = new User { Name = "Reward test", Email = $"{Guid.NewGuid()}@example.test", PasswordHash = "test", PomoBalance = cost * 2 };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        try
        {
            var service = new RewardShopService(db);
            var operationId = Guid.NewGuid();
            Assert.Equal(RewardActionStatus.Success, (await service.PurchaseAsync(user.Id, "pink", operationId)).Status);
            db.ChangeTracker.Clear();
            Assert.Equal(RewardActionStatus.Success, (await service.PurchaseAsync(user.Id, "pink", operationId)).Status);
            db.ChangeTracker.Clear();
            Assert.Equal(RewardActionStatus.InvalidOperation, (await service.PurchaseAsync(user.Id, "blue", operationId)).Status);
            Assert.Equal(cost, await db.Users.Where(item => item.Id == user.Id).Select(item => item.PomoBalance).SingleAsync());
            Assert.Equal(1, await db.UserRewards.CountAsync(item => item.UserId == user.Id && item.OperationId == operationId));
        }
        finally
        {
            await db.Users.Where(item => item.Id == user.Id).ExecuteDeleteAsync();
        }
    }

    [PushDatabaseFact]
    public async Task ResumesAfterInactivityAndAwardsOnlyOnceWhenCompleted()
    {
        var connection = Environment.GetEnvironmentVariable("TOMATIME_PUSH_TEST_CONNECTION")!;
        var parsed = new NpgsqlConnectionStringBuilder(connection);
        Assert.True(parsed.Host is "localhost" or "127.0.0.1" &&
                    parsed.Database!.EndsWith("_test", StringComparison.Ordinal));
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
        var user = new User { Name = "Session test", Email = $"{Guid.NewGuid()}@example.test", PasswordHash = "test" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        try
        {
            var now = DateTime.UtcNow;
            var session = new Session
            {
                UserId = user.Id, PlannedStartTime = now.AddMinutes(-26),
                ActualStartTime = now.AddMinutes(-26), LastActivityAt = now.AddMinutes(-26),
                Duration = 25, Status = SessionStatus.InProgress,
            };
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
            var service = new SessionRunService(db);

            Assert.Null(await service.HeartbeatAsync(user.Id + 1, session.Id));
            var active = await service.HeartbeatAsync(user.Id, session.Id);
            Assert.NotNull(active);
            var cost = await db.Rewards.Where(reward => reward.Id == RewardCatalog.ExtraBreak).Select(reward => reward.Cost).SingleAsync();
            Assert.Equal(cost, active.ExtraBreakCost);
            db.ChangeTracker.Clear();
            Assert.Equal(CompletionStatus.Completed, (await service.CompleteAsync(user.Id, session.Id)).Status);
            db.ChangeTracker.Clear();
            Assert.Equal(CompletionStatus.Completed, (await service.CompleteAsync(user.Id, session.Id)).Status);
            Assert.Equal(2, await db.Users.Where(u => u.Id == user.Id).Select(u => u.PomoBalance).SingleAsync());
        }
        finally
        {
            await db.Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync();
        }
    }

    [PushDatabaseFact]
    public async Task CannotCompleteEarlyOrAfterExplicitCancellation()
    {
        var connection = Environment.GetEnvironmentVariable("TOMATIME_PUSH_TEST_CONNECTION")!;
        var parsed = new NpgsqlConnectionStringBuilder(connection);
        Assert.True(parsed.Host is "localhost" or "127.0.0.1" &&
                    parsed.Database!.EndsWith("_test", StringComparison.Ordinal));
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
        var user = new User { Name = "Session test", Email = $"{Guid.NewGuid()}@example.test", PasswordHash = "test" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        try
        {
            var now = DateTime.UtcNow;
            var session = new Session
            {
                UserId = user.Id, PlannedStartTime = now.AddMinutes(-3),
                ActualStartTime = now.AddMinutes(-3), LastActivityAt = now.AddMinutes(-3),
                Duration = 25, Status = SessionStatus.InProgress,
            };
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
            var service = new SessionRunService(db);

            Assert.Equal(CompletionStatus.TooEarly, (await service.CompleteAsync(user.Id, session.Id)).Status);
            Assert.True(await service.CancelAsync(user.Id, session.Id));
            db.ChangeTracker.Clear();
            Assert.Equal(CompletionStatus.NotFound, (await service.CompleteAsync(user.Id, session.Id)).Status);
            Assert.Equal(0, await db.Users.Where(u => u.Id == user.Id).Select(u => u.PomoBalance).SingleAsync());
        }
        finally
        {
            await db.Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync();
        }
    }

    [PushDatabaseFact]
    public async Task StoredBreakIsAppliedBeforeCompletingAnInactiveSession()
    {
        var connection = Environment.GetEnvironmentVariable("TOMATIME_PUSH_TEST_CONNECTION")!;
        var parsed = new NpgsqlConnectionStringBuilder(connection);
        Assert.True(parsed.Host is "localhost" or "127.0.0.1" &&
                    parsed.Database!.EndsWith("_test", StringComparison.Ordinal));
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
        var user = new User { Name = "Session test", Email = $"{Guid.NewGuid()}@example.test", PasswordHash = "test" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        try
        {
            var now = DateTime.UtcNow;
            var session = new Session
            {
                UserId = user.Id, PlannedStartTime = now.AddMinutes(-57),
                ActualStartTime = now.AddMinutes(-57), LastActivityAt = now.AddMinutes(-57),
                Duration = 50, Status = SessionStatus.InProgress,
            };
            db.Sessions.Add(session);
            db.UserRewards.Add(new UserReward
            {
                OperationId = Guid.NewGuid(), UserId = user.Id, RewardId = RewardCatalog.ExtraBreak,
                Kind = "extra-break", CreatedAt = now.AddMinutes(-58),
            });
            await db.SaveChangesAsync();
            var service = new SessionRunService(db);

            Assert.Equal(CompletionStatus.TooEarly, (await service.CompleteAsync(user.Id, session.Id)).Status);
            db.ChangeTracker.Clear();
            var active = await service.HeartbeatAsync(user.Id, session.Id);
            Assert.NotNull(active);
            Assert.Equal(new[] { 1 }, active.BreakExtensions);
            db.ChangeTracker.Clear();
            Assert.Equal(CompletionStatus.TooEarly, (await service.CompleteAsync(user.Id, session.Id)).Status);
        }
        finally
        {
            await db.Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync();
        }
    }
}
