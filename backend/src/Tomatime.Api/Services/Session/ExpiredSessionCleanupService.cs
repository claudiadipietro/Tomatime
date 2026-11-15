namespace Tomatime.Api.Services;

public sealed class ExpiredSessionCleanupService(
    IServiceScopeFactory scopes, ILogger<ExpiredSessionCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<ISessionRunService>().CancelExpiredAsync();
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Failed to cancel expired sessions.");
            }
        }
    }
}
