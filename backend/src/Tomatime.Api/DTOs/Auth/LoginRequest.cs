using System.ComponentModel.DataAnnotations;

namespace Tomatime.Api.DTOs;

public sealed record LoginRequest(
    [param: Required, EmailAddress, StringLength(320)] string Email,
    [param: Required] string Password);
