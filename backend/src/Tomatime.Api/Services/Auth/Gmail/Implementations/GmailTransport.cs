using System.Net;
using System.Net.Mail;

namespace Tomatime.Api.Services;

public sealed class GmailTransport : IGmailTransport
{
    public async Task SendAsync(MailMessage message, string address, string password, CancellationToken cancellationToken)
    {
        using SmtpClient client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(address, password),
        };
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            await client.SendMailAsync(message, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new PasswordResetEmailDeliveryException("Gmail delivery timed out.");
        }
    }
}
