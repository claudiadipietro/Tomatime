using System.Net.Mail;
using System.Net.Sockets;

namespace Tomatime.Api.Services;

public sealed class PasswordResetEmailSender : IPasswordResetEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly IGmailTransport _transport;

    public PasswordResetEmailSender(IConfiguration configuration, IGmailTransport transport)
    {
        _configuration = configuration;
        _transport = transport;
    }

    public async Task SendAsync(string email, string resetUrl, CancellationToken cancellationToken = default)
    {
        var address = _configuration["Gmail:Address"];
        var password = _configuration["Gmail:AppPassword"];

        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(password))
        {
            throw new PasswordResetEmailDeliveryException("Gmail is not configured.");
        }

        try
        {
            using MailMessage message = new MailMessage
            {
                From = new MailAddress(address, "Tomatime"),
                Subject = "Tomatime - Restablecer contraseña",
                Body = "Hemos recibido una solicitud para restablecer tu contraseña de Tomatime.\n\n" +
                    $"Abre este enlace para elegir una nueva contraseña:\n{resetUrl}\n\n" +
                    "El enlace caduca en 30 minutos. Si no has solicitado este cambio, ignora este correo.",
            };
            message.To.Add(new MailAddress(email));
            await _transport.SendAsync(message, address, password, cancellationToken);
        }
        catch (Exception exception) when (exception is SmtpException or FormatException or IOException or SocketException)
        {
            throw new PasswordResetEmailDeliveryException("Gmail delivery failed.");
        }
    }
}
