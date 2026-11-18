namespace Tomatime.Api.Services;

public sealed record PasswordResetEmail(string Address, string ResetUrl);
