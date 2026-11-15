using System.Threading.Channels;

namespace Tomatime.Api.Services;

public sealed record PasswordResetEmail(string Address, string ResetUrl);

public sealed class PasswordResetEmailQueue
{
    private readonly Channel<PasswordResetEmail> _channel = Channel.CreateBounded<PasswordResetEmail>(
        new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });

    public bool TryEnqueue(PasswordResetEmail email) => _channel.Writer.TryWrite(email);

    public IAsyncEnumerable<PasswordResetEmail> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

public sealed class PasswordResetEmailWorker(
    PasswordResetEmailQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<PasswordResetEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var email in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IPasswordResetEmailSender>();
                await sender.SendAsync(email.Address, email.ResetUrl, stoppingToken);
            }
            catch (PasswordResetEmailDeliveryException exception)
            {
                logger.LogError("Password reset email could not be sent: {Reason}", exception.Message);
            }
        }
    }
}
