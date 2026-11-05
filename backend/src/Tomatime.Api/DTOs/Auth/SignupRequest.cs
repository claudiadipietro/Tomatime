using System.ComponentModel.DataAnnotations;

namespace Tomatime.Api.DTOs;

public sealed record SignupRequest(
    [param: Required, StringLength(100, MinimumLength = 2)] string Name,
    [param: Required, EmailAddress, StringLength(320)] string Email,
    [param: Required, MinLength(8), StringLength(128)] string Password);
