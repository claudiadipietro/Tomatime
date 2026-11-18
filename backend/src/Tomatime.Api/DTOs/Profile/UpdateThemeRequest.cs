using System.ComponentModel.DataAnnotations;

namespace Tomatime.Api.DTOs;

public sealed record UpdateThemeRequest([param: Required] string Theme);
