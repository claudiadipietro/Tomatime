using System.ComponentModel.DataAnnotations;

namespace Tomatime.Api.DTOs;

public sealed record ForgotPasswordRequest(
    [param: Required, EmailAddress, StringLength(320)] string Email);
