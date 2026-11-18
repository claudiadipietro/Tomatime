using System.ComponentModel.DataAnnotations;

namespace Tomatime.Api.DTOs;

public sealed record NotificationPreferenceRequest([param: Required] bool? Enabled);
