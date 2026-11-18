using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tomatime.Api.Context;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class SessionReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IWebPushSender _sender;
    private readonly ReminderWakeup _wakeup;
    private readonly ILogger<SessionReminderService> _logger;

    public SessionReminderService(
        IServiceScopeFactory scopes, IWebPushSender sender, ReminderWakeup wakeup, ILogger<SessionReminderService> logger)
    {
        _scopes = scopes;
        _sender = sender;
        _wakeup = wakeup;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            try
            {
                using IServiceScope scope = _scopes.CreateScope();
                AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                PushSubscriptionService push = scope.ServiceProvider.GetRequiredService<PushSubscriptionService>();
                if (push.Configured)
                {
                    await SendDueAsync(db, stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Failed to process session reminders.");
            }
            await _wakeup.WaitAsync(stoppingToken);
        } while (!stoppingToken.IsCancellationRequested);
    }

    public async Task SendDueAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        List<int> sessions = await db.Sessions.AsNoTracking()
            .Where(s => s.Status == SessionStatus.Planned && s.PlannedStartTime > now &&
                s.PlannedStartTime <= now.AddMinutes(5))
            .OrderBy(s => s.PlannedStartTime).Select(s => s.Id).ToListAsync(cancellationToken);

        foreach (var sessionId in sessions)
        {
            await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(cancellationToken);

            Session? session = await db.Sessions.FromSqlInterpolated(
                $"SELECT * FROM \"Sessions\" WHERE \"Id\" = {sessionId} FOR UPDATE SKIP LOCKED")
                .SingleOrDefaultAsync(cancellationToken);

            if (session is null || session.Status != SessionStatus.Planned || session.PlannedStartTime <= DateTime.UtcNow)
            {
                continue;
            }

            User? user = await db.Users.FromSqlInterpolated(
                $"SELECT * FROM \"Users\" WHERE \"Id\" = {session.UserId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);

            if (user is null || !user.NotificationsEnabled)
            {
                continue;
            }

            List<PushDevice> devices = await db.PushDevices.FromSqlInterpolated(
                $"SELECT * FROM \"PushDevices\" WHERE \"UserId\" = {user.Id} FOR UPDATE")
                .ToListAsync(cancellationToken);

            List<int> sent = await db.ReminderDeliveries.Where(d => d.SessionId == sessionId)
                .Select(d => d.PushDeviceId).ToListAsync(cancellationToken);

            foreach (var device in devices.Where(d => !sent.Contains(d.Id)))
            {
                int ttl = (int)Math.Floor((session.PlannedStartTime - DateTime.UtcNow).TotalSeconds);
                if (ttl <= 0)
                {
                    break;
                }

                string title = device.Language == "es" ? "Tu sesión está por comenzar" : "Your session is starting soon";
                string body = device.Language == "es" ? "Tu sesión de concentración comienza pronto." : "Your focus session starts soon.";
                string payload = JsonSerializer.Serialize(new
                {
                    notification = new
                    {
                        title, body, icon = "/icons/icon-192x192.png",
                        tag = $"session-reminder-{session.Id}",
                        data = new { onActionClick = new Dictionary<string, object> { ["default"] = new { operation = "focusLastFocusedOrOpen", url = "/sessions/calendar" } } }
                    }
                });
                try
                {
                    using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeout.CancelAfter(TimeSpan.FromSeconds(10));
                    PushDeliveryResult result = await _sender.SendAsync(device, payload, ttl, timeout.Token);
                    if (result == PushDeliveryResult.Expired)
                    {
                        db.PushDevices.Remove(device);
                    }
                    else
                    {
                        db.ReminderDeliveries.Add(new ReminderDelivery { SessionId = session.Id, PushDeviceId = device.Id, SentAt = DateTime.UtcNow });
                    }
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Could not send session reminder for session {SessionId}, device {DeviceId}: {ErrorType}.",
                        session.Id, device.Id, ex.GetType().Name);
                }
                await db.SaveChangesAsync(cancellationToken);
            }
            await tx.CommitAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }
    }
}
