using Tomatime.Api.Services;

namespace Tomatime.Api.Tests;

public class PushSubscriptionTests
{
    private static readonly string Key = Convert.ToBase64String(new byte[65]).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static readonly string Auth = Convert.ToBase64String(new byte[16]).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [Theory]
    [InlineData("https://fcm.googleapis.com/fcm/send/abc", true)]
    [InlineData("https://updates.push.services.mozilla.com/wpush/v2/abc", true)]
    [InlineData("https://web.push.apple.com/abc", true)]
    [InlineData("https://abc.push.apple.com/abc", true)]
    [InlineData("http://fcm.googleapis.com/fcm/send/abc", false)]
    [InlineData("https://fcm.googleapis.com.evil.example/abc", false)]
    [InlineData("https://127.0.0.1/abc", false)]
    [InlineData("https://fcm.googleapis.com:444/abc", false)]
    [InlineData("https://user@fcm.googleapis.com/abc", false)]
    [InlineData("https://fcm.googleapis.com/abc#fragment", false)]
    [InlineData("https://fcm.googleapis.com/á", false)]
    [InlineData("https://evilpush.apple.com/abc", false)]
    [InlineData("https://fcm.googleapis.com:443/abc", true)]
    public void AllowsOnlyKnownSecurePushProviders(string endpoint, bool valid) =>
        Assert.Equal(valid, PushSubscriptionService.IsValid(new(endpoint, Key, Auth, "es")));

    [Fact]
    public void RejectsEndpointsThatExceedTheLengthLimit()
    {
        var endpoint = "https://fcm.googleapis.com/" + new string('a', 2049);
        Assert.False(PushSubscriptionService.IsValid(new(endpoint, Key, Auth, "es")));
    }

    [Fact]
    public void RejectsMalformedKeysAndUnsupportedLanguages()
    {
        Assert.False(PushSubscriptionService.IsValid(new("https://fcm.googleapis.com/a", "bad", Auth, "es")));
        Assert.False(PushSubscriptionService.IsValid(new("https://fcm.googleapis.com/a", Key, Auth, "fr")));
    }

    [Fact]
    public async Task WakeupCoalescesRepeatedTriggers()
    {
        var wakeup = new ReminderWakeup();
        wakeup.Trigger();
        wakeup.Trigger();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        Assert.True(await wakeup.WaitAsync(timeout.Token));
    }
}
