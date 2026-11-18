using System.ComponentModel.DataAnnotations;

namespace Tomatime.Api.DTOs;

public sealed record ChangePasswordRequest(
    [param: Required, StringLength(128)] string CurrentPassword,
    [param: Required, MinLength(8), StringLength(128), RegularExpression(@".*\S.*")] string NewPassword);
