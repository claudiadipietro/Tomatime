using Microsoft.EntityFrameworkCore;
using Tomatime.Api.Context;
using Tomatime.Api.Models;

namespace Tomatime.Api.Services;

public sealed record PushDeviceRequest(string Endpoint, string P256dh, string Auth, string Language);

public sealed class PushSubscriptionService(AppDbContext db, IConfiguration configuration)
{
    public string? PublicKey => configuration["WebPush:PublicKey"];
    public bool Configured => !string.IsNullOrWhiteSpace(PublicKey)
        && !string.IsNullOrWhiteSpace(configuration["WebPush:PrivateKey"])
        && !string.IsNullOrWhiteSpace(configuration["WebPush:Subject"]);

    public static bool IsValid(PushDeviceRequest device)
    {
        if (!Uri.TryCreate(device.Endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment) ||
            device.Endpoint.Length > 2048 || device.Endpoint.Any(c => c > 127 || char.IsControl(c))) return false;
        var host = uri.IdnHost.ToLowerInvariant();
        if (host != "fcm.googleapis.com" && host != "updates.push.services.mozilla.com" &&
            host != "web.push.apple.com" && !host.EndsWith(".push.apple.com", StringComparison.Ordinal)) return false;
        return IsBase64Url(device.P256dh, 65) && IsBase64Url(device.Auth, 16) &&
            device.Language is "en" or "es";
    }

    private static bool IsBase64Url(string? value, int expectedLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 128 ||
            value.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_')) return false;
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
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "PushDevices" ("UserId", "Endpoint", "P256dh", "Auth", "Language")
            VALUES ({userId}, {request.Endpoint}, {request.P256dh}, {request.Auth}, {request.Language})
            ON CONFLICT ("Endpoint") DO UPDATE SET "UserId" = EXCLUDED."UserId",
                "P256dh" = EXCLUDED."P256dh", "Auth" = EXCLUDED."Auth", "Language" = EXCLUDED."Language"
            """);
    }

    public Task<int> RemoveAsync(int userId, string endpoint) =>
        db.PushDevices.Where(device => device.UserId == userId && device.Endpoint == endpoint).ExecuteDeleteAsync();

    public Task<bool> IsRegisteredAsync(int userId, string endpoint) =>
        db.PushDevices.AnyAsync(device => device.UserId == userId && device.Endpoint == endpoint);
}
