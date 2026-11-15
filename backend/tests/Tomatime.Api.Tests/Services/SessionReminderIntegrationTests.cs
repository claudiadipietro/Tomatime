using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Tomatime.Api.Context;
using Tomatime.Api.Models;
using Tomatime.Api.Services;

namespace Tomatime.Api.Tests;

public sealed class PushDatabaseFactAttribute : FactAttribute
{
    public PushDatabaseFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TOMATIME_PUSH_TEST_CONNECTION")))
            Skip = "Set TOMATIME_PUSH_TEST_CONNECTION to an isolated local test database.";
    }
}

public class SessionReminderIntegrationTests
{
    [PushDatabaseFact]
    public async Task SendsDueRemindersOncePerDeviceAndRetriesFailures()
    {
        var connection = Environment.GetEnvironmentVariable("TOMATIME_PUSH_TEST_CONNECTION")!;
        var parsed = new NpgsqlConnectionStringBuilder(connection);
        Assert.True(parsed.Host is "localhost" or "127.0.0.1" && parsed.Database!.EndsWith("_test", StringComparison.Ordinal));
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
        var user = new User { Name = "Push test", Email = $"{Guid.NewGuid()}@example.test", PasswordHash = "test", NotificationsEnabled = true };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        try
        {
            var now = DateTime.UtcNow;
            var due = new Session { UserId = user.Id, PlannedStartTime = now.AddMinutes(4), Duration = 25, Status = SessionStatus.Planned };
            var immediate = new Session { UserId = user.Id, PlannedStartTime = now.AddMinutes(1), Duration = 25, Status = SessionStatus.Planned };
            var later = new Session { UserId = user.Id, PlannedStartTime = now.AddMinutes(6), Duration = 25, Status = SessionStatus.Planned };
            var started = new Session { UserId = user.Id, PlannedStartTime = now.AddMinutes(1), Duration = 25, Status = SessionStatus.InProgress };
            var past = new Session { UserId = user.Id, PlannedStartTime = now.AddMinutes(-1), Duration = 25, Status = SessionStatus.Planned };
            db.Sessions.AddRange(due, immediate, later, started, past);
            db.PushDevices.Add(new PushDevice { UserId = user.Id, Endpoint = "https://fcm.googleapis.com/test-" + Guid.NewGuid(), P256dh = "test", Auth = "test", Language = "es" });
            await db.SaveChangesAsync();
            var sender = new FakeSender { Fail = true };
            using var provider = new ServiceCollection().BuildServiceProvider();
            var worker = new SessionReminderService(provider.GetRequiredService<IServiceScopeFactory>(), sender, new ReminderWakeup(), NullLogger<SessionReminderService>.Instance);
            await worker.SendDueAsync(db, CancellationToken.None);
            Assert.Empty(await db.ReminderDeliveries.ToListAsync());
            sender.Fail = false;
            await worker.SendDueAsync(db, CancellationToken.None);
            Assert.Equal(2, await db.ReminderDeliveries.CountAsync());
            Assert.Equal(2, sender.Sent.Count);
            Assert.All(sender.Sent, payload =>
            {
                using var document = JsonDocument.Parse(payload);
                Assert.Contains("Tu sesión", document.RootElement.GetProperty("notification").GetProperty("title").GetString());
            });
            using var json = JsonDocument.Parse(sender.Sent[0]);
            var click = json.RootElement.GetProperty("notification").GetProperty("data")
                .GetProperty("onActionClick").GetProperty("default");
            Assert.Equal("focusLastFocusedOrOpen", click.GetProperty("operation").GetString());
            Assert.Equal("/sessions/calendar", click.GetProperty("url").GetString());
            await worker.SendDueAsync(db, CancellationToken.None);
            Assert.Equal(2, sender.Sent.Count);
            db.PushDevices.Add(new PushDevice { UserId = user.Id, Endpoint = "https://fcm.googleapis.com/test-" + Guid.NewGuid(), P256dh = "test", Auth = "test", Language = "en" });
            await db.SaveChangesAsync();
            await worker.SendDueAsync(db, CancellationToken.None);
            Assert.Equal(4, sender.Sent.Count);
            Assert.Equal(4, await db.ReminderDeliveries.CountAsync());
            await db.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.NotificationsEnabled, false));
            later.PlannedStartTime = now.AddMinutes(2);
            db.Sessions.Attach(later);
            db.Entry(later).Property(s => s.PlannedStartTime).IsModified = true;
            await db.SaveChangesAsync();
            await worker.SendDueAsync(db, CancellationToken.None);
            Assert.Equal(4, sender.Sent.Count);
        }
        finally
        {
            await db.Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync();
        }
    }

    private sealed class FakeSender : IWebPushSender
    {
        public bool Fail { get; set; }
        public List<string> Sent { get; } = [];
        public Task<PushDeliveryResult> SendAsync(PushDevice device, string payload, int ttl, CancellationToken cancellationToken)
        {
            Assert.InRange(ttl, 1, 300);
            if (Fail) throw new HttpRequestException("Temporary failure");
            Sent.Add(payload);
            return Task.FromResult(PushDeliveryResult.Sent);
        }
    }
}
