using System.Net;
using Tomatime.Api.Models;
using WebPush;

namespace Tomatime.Api.Services;

public sealed class WebPushSender : IWebPushSender
{
    private readonly IConfiguration _configuration;

    public WebPushSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<PushDeliveryResult> SendAsync(PushDevice device, string payload, int ttl, CancellationToken cancellationToken)
    {
        WebPushClient client = new WebPushClient();
        PushSubscription subscription = new PushSubscription(device.Endpoint, device.P256dh, device.Auth);
        Dictionary<string, object> options = new Dictionary<string, object>
        {
            ["vapidDetails"] = new VapidDetails(_configuration["WebPush:Subject"], _configuration["WebPush:PublicKey"], _configuration["WebPush:PrivateKey"]),
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
