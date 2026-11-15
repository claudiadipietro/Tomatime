using System.Net;
using Tomatime.Api.Models;
using WebPush;

namespace Tomatime.Api.Services;

public enum PushDeliveryResult { Sent, Expired }

public interface IWebPushSender
{
    Task<PushDeliveryResult> SendAsync(PushDevice device, string payload, int ttl, CancellationToken cancellationToken);
}

public sealed class WebPushSender(IConfiguration configuration) : IWebPushSender
{
    public async Task<PushDeliveryResult> SendAsync(PushDevice device, string payload, int ttl, CancellationToken cancellationToken)
    {
        var client = new WebPushClient();
        var subscription = new PushSubscription(device.Endpoint, device.P256dh, device.Auth);
        var options = new Dictionary<string, object>
        {
            ["vapidDetails"] = new VapidDetails(configuration["WebPush:Subject"], configuration["WebPush:PublicKey"], configuration["WebPush:PrivateKey"]),
            ["TTL"] = ttl,
        };
        try
        {
            await client.SendNotificationAsync(subscription, payload, options, cancellationToken);
            return PushDeliveryResult.Sent;
        }
        catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
        {
            return PushDeliveryResult.Expired;
        }
    }
}
