namespace Tomatime.Api.Services;

public interface IPasswordResetEmailSender
{
    Task SendAsync(string email, string resetUrl, CancellationToken cancellationToken = default);
}
