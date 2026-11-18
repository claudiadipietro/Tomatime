using System.Net.Mail;

namespace Tomatime.Api.Services;

public interface IGmailTransport
{
    Task SendAsync(MailMessage message, string address, string password, CancellationToken cancellationToken);
}
