using System.Collections.Concurrent;

namespace Tomatime.Api.Services;

public sealed class SessionTestClock(bool enabled)
{
    private readonly ConcurrentDictionary<int, int> _seconds = new();

    public bool Enabled { get; } = enabled;

    public DateTime UtcNow(int sessionId) =>
        DateTime.UtcNow.AddSeconds(Enabled ? _seconds.GetValueOrDefault(sessionId) : 0);

    public void Advance(int sessionId, int seconds)
    {
        if (!Enabled)
        {
            throw new InvalidOperationException("The session test clock is disabled.");
        }
        if (seconds is < 1 or > 3600)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }
        _seconds.AddOrUpdate(sessionId, seconds, (_, current) => checked(current + seconds));
    }
}
