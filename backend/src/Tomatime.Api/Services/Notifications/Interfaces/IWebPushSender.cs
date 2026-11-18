using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public interface IWebPushSender
{
    Task<PushDeliveryResult> SendAsync(PushDevice device, string payload, int ttl, CancellationToken cancellationToken);
}
