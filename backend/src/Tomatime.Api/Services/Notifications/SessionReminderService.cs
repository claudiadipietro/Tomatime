using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tomatime.Api.Context;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class SessionReminderService(
    IServiceScopeFactory scopes, IWebPushSender sender, ReminderWakeup wakeup, ILogger<SessionReminderService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var push = scope.ServiceProvider.GetRequiredService<PushSubscriptionService>();
                if (push.Configured) await SendDueAsync(db, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Failed to process session reminders.");
            }
            await wakeup.WaitAsync(stoppingToken);
        } while (!stoppingToken.IsCancellationRequested);
    }

    public async Task SendDueAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var sessions = await db.Sessions.AsNoTracking()
            .Where(s => s.Status == SessionStatus.Planned && s.PlannedStartTime > now &&
                s.PlannedStartTime <= now.AddMinutes(5))
            .OrderBy(s => s.PlannedStartTime).Select(s => s.Id).ToListAsync(cancellationToken);

        foreach (var sessionId in sessions)
        {
            await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
            var session = await db.Sessions.FromSqlInterpolated(
                $"SELECT * FROM \"Sessions\" WHERE \"Id\" = {sessionId} FOR UPDATE SKIP LOCKED")
                .SingleOrDefaultAsync(cancellationToken);
            if (session is null || session.Status != SessionStatus.Planned || session.PlannedStartTime <= DateTime.UtcNow)
                continue;
            var user = await db.Users.FromSqlInterpolated(
                $"SELECT * FROM \"Users\" WHERE \"Id\" = {session.UserId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (user is null || !user.NotificationsEnabled) continue;

            var devices = await db.PushDevices.FromSqlInterpolated(
                $"SELECT * FROM \"PushDevices\" WHERE \"UserId\" = {user.Id} FOR UPDATE")
                .ToListAsync(cancellationToken);
            var sent = await db.ReminderDeliveries.Where(d => d.SessionId == sessionId)
                .Select(d => d.PushDeviceId).ToListAsync(cancellationToken);
            foreach (var device in devices.Where(d => !sent.Contains(d.Id)))
            {
                var ttl = (int)Math.Floor((session.PlannedStartTime - DateTime.UtcNow).TotalSeconds);
                if (ttl <= 0) break;
                var title = device.Language == "es" ? "Tu sesión está por comenzar" : "Your session is starting soon";
                var body = device.Language == "es" ? "Tu sesión de concentración comienza pronto." : "Your focus session starts soon.";
                var payload = JsonSerializer.Serialize(new
                {
                    notification = new
                    {
                        title, body, icon = "/icons/icon-192x192.png",
                        tag = $"session-reminder-{session.Id}",
                        data = new { onActionClick = new Dictionary<string, object> { ["default"] = new { operation = "openWindow", url = "/sessions/calendar" } } }
                    }
                });
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeout.CancelAfter(TimeSpan.FromSeconds(10));
                    var result = await sender.SendAsync(device, payload, ttl, timeout.Token);
                    if (result == PushDeliveryResult.Expired) db.PushDevices.Remove(device);
                    else db.ReminderDeliveries.Add(new ReminderDelivery { SessionId = session.Id, PushDeviceId = device.Id, SentAt = DateTime.UtcNow });
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning("Could not send session reminder for session {SessionId}, device {DeviceId}: {ErrorType}.",
                        session.Id, device.Id, ex.GetType().Name);
                }
                await db.SaveChangesAsync(cancellationToken);
            }
            await tx.CommitAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }
    }
}
