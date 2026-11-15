namespace Tomatime.Api.Services;

public sealed class ReminderWakeup
{
    private readonly SemaphoreSlim signal = new(0, 1);

    public void Trigger()
    {
        if (signal.CurrentCount == 0)
        {
            try { signal.Release(); }
            catch (SemaphoreFullException) { }
        }
    }

    public Task<bool> WaitAsync(CancellationToken cancellationToken) =>
        signal.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
}
