using System.Net;
using System.Net.Mail;
using System.Net.Sockets;

namespace Tomatime.Api.Services;

public sealed class PasswordResetEmailSender(
    IConfiguration configuration,
    IGmailTransport transport) : IPasswordResetEmailSender
{
    public async Task SendAsync(string email, string resetUrl, CancellationToken cancellationToken = default)
    {
        var address = configuration["Gmail:Address"];
        var password = configuration["Gmail:AppPassword"];

        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(password))
        {
            throw new PasswordResetEmailDeliveryException("Gmail is not configured.");
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(address, "Tomatime"),
                Subject = "Tomatime - Restablecer contraseña",
                Body = "Hemos recibido una solicitud para restablecer tu contraseña de Tomatime.\n\n" +
                    $"Abre este enlace para elegir una nueva contraseña:\n{resetUrl}\n\n" +
                    "El enlace caduca en 30 minutos. Si no has solicitado este cambio, ignora este correo.",
            };
            message.To.Add(new MailAddress(email));
            await transport.SendAsync(message, address, password, cancellationToken);
        }
        catch (Exception exception) when (exception is SmtpException or FormatException or IOException or SocketException)
        {
            // SMTP errors may contain addresses or server responses; do not expose them in logs.
            throw new PasswordResetEmailDeliveryException("Gmail delivery failed.");
        }
    }
}

public sealed class PasswordResetEmailDeliveryException(string message) : Exception(message);

public interface IGmailTransport
{
    Task SendAsync(MailMessage message, string address, string password, CancellationToken cancellationToken);
}

public sealed class GmailTransport : IGmailTransport
{
    public async Task SendAsync(MailMessage message, string address, string password, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(address, password),
        };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
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
