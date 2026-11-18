namespace Tomatime.Api.Services;

public sealed class PasswordResetEmailWorker : BackgroundService
{
    private readonly PasswordResetEmailQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PasswordResetEmailWorker> _logger;

    public PasswordResetEmailWorker(
        PasswordResetEmailQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<PasswordResetEmailWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (PasswordResetEmail email in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                IPasswordResetEmailSender sender = scope.ServiceProvider.GetRequiredService<IPasswordResetEmailSender>();
                await sender.SendAsync(email.Address, email.ResetUrl, stoppingToken);
            }
            catch (PasswordResetEmailDeliveryException exception)
            {
                _logger.LogError("Password reset email could not be sent: {Reason}", exception.Message);
            }
        }
    }
}
