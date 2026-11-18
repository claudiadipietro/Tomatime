namespace Tomatime.Api.Services;

public sealed class ReminderWakeup
{
    private readonly SemaphoreSlim _signal = new(0, 1);

    public void Trigger()
    {
        if (_signal.CurrentCount == 0)
        {
            try { _signal.Release(); }
            catch (SemaphoreFullException) { }
        }
    }

    public Task<bool> WaitAsync(CancellationToken cancellationToken) =>
        _signal.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
}
