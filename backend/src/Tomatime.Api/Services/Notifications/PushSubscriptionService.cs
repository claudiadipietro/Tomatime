using Microsoft.EntityFrameworkCore;
using Tomatime.Api.Context;
using Tomatime.Api.DTOs;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed class PushSubscriptionService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public PushSubscriptionService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public string? PublicKey => _configuration["WebPush:PublicKey"];
    public bool Configured => !string.IsNullOrWhiteSpace(PublicKey)
        && !string.IsNullOrWhiteSpace(_configuration["WebPush:PrivateKey"])
        && !string.IsNullOrWhiteSpace(_configuration["WebPush:Subject"]);

    public static bool IsValid(PushDeviceRequest device) =>
        IsValidEndpoint(device.Endpoint)
        && IsBase64Url(device.P256dh, 65)
        && IsBase64Url(device.Auth, 16)
        && device.Language is "en" or "es";

    private static bool IsValidEndpoint(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (endpoint.Length > 2048 || endpoint.Any(c => c > 127 || char.IsControl(c)))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        string host = uri.IdnHost.ToLowerInvariant();
        return host is "fcm.googleapis.com" or "updates.push.services.mozilla.com" or "web.push.apple.com"
            || host.EndsWith(".push.apple.com", StringComparison.Ordinal);
    }

    private static bool IsBase64Url(string? value, int expectedLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 128 ||
            value.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
        {
            return false;
        }

        try
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight((padded.Length + 3) / 4 * 4, '=');
            return Convert.FromBase64String(padded).Length == expectedLength;
        }
        catch (FormatException) { return false; }
    }

    public async Task SaveAsync(int userId, PushDeviceRequest request)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "PushDevices" ("UserId", "Endpoint", "P256dh", "Auth", "Language")
            VALUES ({userId}, {request.Endpoint}, {request.P256dh}, {request.Auth}, {request.Language})
            ON CONFLICT ("Endpoint") DO UPDATE SET "UserId" = EXCLUDED."UserId",
                "P256dh" = EXCLUDED."P256dh", "Auth" = EXCLUDED."Auth", "Language" = EXCLUDED."Language"
            """);
    }

    public Task<int> RemoveAsync(int userId, string endpoint) =>
        _db.PushDevices.Where(device => device.UserId == userId && device.Endpoint == endpoint).ExecuteDeleteAsync();

    public Task<bool> IsRegisteredAsync(int userId, string endpoint) =>
        _db.PushDevices.AnyAsync(device => device.UserId == userId && device.Endpoint == endpoint);
}
