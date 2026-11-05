using System.ComponentModel.DataAnnotations;

namespace Tomatime.Api.DTOs;

public sealed record ResetPasswordRequest(
    [param: Required] string Token,
    [param: Required, MinLength(8), StringLength(128)] string NewPassword);
