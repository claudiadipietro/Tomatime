using System.Threading.Channels;

namespace Tomatime.Api.Services;

public sealed class PasswordResetEmailQueue
{
    private readonly Channel<PasswordResetEmail> _channel = Channel.CreateBounded<PasswordResetEmail>(
        new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });

    public bool TryEnqueue(PasswordResetEmail email) => _channel.Writer.TryWrite(email);

    public IAsyncEnumerable<PasswordResetEmail> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
