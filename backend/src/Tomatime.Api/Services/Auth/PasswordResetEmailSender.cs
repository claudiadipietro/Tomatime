namespace Tomatime.Api.Services;

public sealed class PasswordResetEmailSender: IPasswordResetEmailSender
{
    private readonly ILogger<PasswordResetEmailSender> _logger;

    public PasswordResetEmailSender(
        ILogger<PasswordResetEmailSender> logger)
    {
        _logger = logger;
    }
    public Task SendAsync(
        string email,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement actual email sending logic here. For now, we just log the reset URL.
        _logger.LogInformation(
            "Password reset link generated for {Email}: {ResetUrl}",
            email,
            resetUrl);

        return Task.CompletedTask;
    }
}
