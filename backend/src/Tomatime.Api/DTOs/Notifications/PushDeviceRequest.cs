namespace Tomatime.Api.DTOs;

public sealed record PushDeviceRequest(string Endpoint, string P256dh, string Auth, string Language);
