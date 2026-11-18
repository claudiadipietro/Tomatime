namespace Tomatime.Api.Services;

public sealed class PasswordResetEmailDeliveryException(string message) : Exception(message);
