using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Swagger;
using Tomatime.Api.Controllers;
using Tomatime.Api.DTOs;
using Tomatime.Api.Models;
using Tomatime.Api.Services;

namespace Tomatime.Api.Tests;

public class LegacyEndpointSecurityTests
{
    private const string SigningKey = "test-only-signing-key-with-at-least-32-characters";

    [Fact]
    public async Task LegacyRoutesRequireAuthenticationAndEnforceOwnership()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Production",
            ApplicationName = typeof(UserController).Assembly.FullName,
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(UserController).Assembly);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSingleton<IProfileService, FakeProfile>();
        builder.Services.AddSingleton<IUserRewardDatabaseService, FakeRewards>();
        builder.Services.AddSingleton<ISessionDatabaseService, FakeSessions>();
        builder.Services.AddSingleton<IThemeDatabaseService, FakeThemes>();
        builder.Services.AddSingleton<IRewardDatabaseService, FakeCatalog>();
        builder.Services.AddSingleton<FakeFeatures>();
        builder.Services.AddSingleton<IRewardShopService>(services => services.GetRequiredService<FakeFeatures>());
        builder.Services.AddSingleton<ISessionCalendarService>(services => services.GetRequiredService<FakeFeatures>());
        builder.Services.AddSingleton<ISessionHistoryService>(services => services.GetRequiredService<FakeFeatures>());
        builder.Services.AddSingleton<ISessionPlanningService>(services => services.GetRequiredService<FakeFeatures>());
        builder.Services.AddSingleton<ISessionStartService>(services => services.GetRequiredService<FakeFeatures>());
        builder.Services.AddSingleton<ISessionRunService>(services => services.GetRequiredService<FakeFeatures>());
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
            });
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        var swagger = app.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        Assert.Contains("/api/sessions", swagger.Paths.Keys);
        Assert.Contains("/api/rewards/{rewardId}/redeem", swagger.Paths.Keys);
        foreach (var route in new[]
        {
            "/api/users/profile", "/api/users/notifications", "/api/users/password",
            "/api/sessions/calendar", "/api/sessions/history", "/api/sessions/history/{sessionId}",
            "/api/sessions/{sessionId}/start", "/api/sessions/{sessionId}/heartbeat",
            "/api/sessions/{sessionId}/cancel", "/api/sessions/{sessionId}/complete",
            "/api/sessions/{sessionId}/extra-break",
        })
        {
            Assert.Contains(route, swagger.Paths.Keys);
        }
        Assert.DoesNotContain(swagger.Paths.Keys, path =>
            path.StartsWith("/api/profile") || path.StartsWith("/api/session-") ||
            path.StartsWith("/api/reward-shop"));
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };

        foreach (var route in new[] { "/api/users/profile/7", "/api/users/7/rewards", "/api/users/notifications/7", "/api/sessions", "/api/sessions/1", "/api/users/profile", "/api/users/rewards", "/api/sessions/history" })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);
        }

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PutAsJsonAsync("/api/users/notifications/7", true)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/rewards/-1/redeem", new { operationId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PostAsJsonAsync("/api/reward-shop/pink/purchase", new { operationId = Guid.NewGuid() })).StatusCode);
        var catalog = await client.GetFromJsonAsync<List<Reward>>("/api/rewards");
        Assert.Equal(5, Assert.Single(catalog!).Cost);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/sessions")).StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(7));
        using var profile = await client.GetAsync("/api/users/profile/7");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        var json = JsonDocument.Parse(await profile.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Jane", json.GetProperty("name").GetString());
        Assert.False(json.TryGetProperty("passwordHash", out _));
        Assert.False(json.TryGetProperty("PasswordHash", out _));

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/profile/8")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/8/rewards")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/notifications/8")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync("/api/users/notifications/8", true)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/7/rewards")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/notifications/7")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/users/notifications/7", true)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/notifications")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/profile/notifications", new { enabled = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/profile/password", new { currentPassword = "old", newPassword = "new" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/rewards")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/reward-shop")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/sessions/history")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/session-history")).StatusCode);
        var operationId = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/rewards/-1/redeem", new { operationId })).StatusCode);
        var features = app.Services.GetRequiredService<FakeFeatures>();
        Assert.Equal("pink", features.LastKind);
        Assert.Equal(operationId, features.LastOperationId);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/reward-shop/pink/purchase", new { operationId })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/reward-shop/themes/pink/activate", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/rewards/999/redeem", new { operationId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/users/theme", new { theme = "pink" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/sessions", new { duration = 50 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/sessions", new { duration = 50, plannedStartTime = DateTimeOffset.UtcNow.AddDays(1) })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/session-starts", new { duration = 50 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/session-plans", new { duration = 50, plannedStartTime = DateTimeOffset.UtcNow.AddDays(1) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/sessions", new { duration = 24 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/sessions/1/start", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/session-starts/1", new { })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/sessions/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/sessions/2")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/sessions/999")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/sessions?userId=8")).StatusCode);
        using var list = await client.GetAsync("/api/sessions");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var sessions = await list.Content.ReadFromJsonAsync<List<Session>>();
        Assert.Equal(7, Assert.Single(sessions!).UserId);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/sessions?userId=7")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(8));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/sessions/1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/sessions/2")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/profile/7")).StatusCode);

        foreach (var token in new[] { null, CreateToken(7) })
        {
            client.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/users/email/person@example.com")).StatusCode);
            Assert.True((await client.DeleteAsync("/api/users/7")).StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.PostAsJsonAsync("/api/themes", new { name = "test" })).StatusCode);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.PutAsJsonAsync("/api/themes/1", new { id = 1, name = "test" })).StatusCode);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.DeleteAsync("/api/themes/1")).StatusCode);
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/themes")).StatusCode);
    }

    private static string CreateToken(int userId) => new JwtSecurityTokenHandler().WriteToken(
        new JwtSecurityToken(
            claims: [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256)));

    private sealed class FakeProfile : IProfileService
    {
        public Task<ProfileInfo?> GetAsync(int userId) => Task.FromResult<ProfileInfo?>(new("Jane", "jane@example.com", 5, "default", true));
        public Task<bool> UpdateNotificationsAsync(int userId, bool enabled) => Task.FromResult(true);
        public Task<PasswordChangeStatus> ChangePasswordAsync(int userId, string currentPassword, string newPassword) => throw new NotImplementedException();
    }

    private sealed class FakeRewards : IUserRewardDatabaseService
    {
        public Task<List<UserReward>> GetUserRewardsByUserIdAsync(int userId) => Task.FromResult(new List<UserReward> { new() { UserId = userId } });
        public Task<UserReward?> GetUserRewardByIdAsync(int userRewardId) => throw new NotImplementedException();
        public Task<UserReward?> CreateUserRewardAsync(UserReward userReward) => throw new NotImplementedException();
        public Task<UserReward?> UpdateUserRewardAsync(UserReward userReward) => throw new NotImplementedException();
        public Task<bool> DeleteUserRewardAsync(int userRewardId) => throw new NotImplementedException();
    }

    private sealed class FakeSessions : ISessionDatabaseService
    {
        public Task<Session?> GetSessionByIdAsync(int sessionId) => Task.FromResult(sessionId switch
        {
            1 => new Session { Id = 1, UserId = 7 },
            2 => new Session { Id = 2, UserId = 8 },
            _ => null,
        });
        public Task<List<Session>> GetSessionsByUserIdAsync(int userId) => Task.FromResult(new List<Session> { new() { Id = 1, UserId = userId } });
        public Task<Session?> CreateSessionAsync(Session session) => throw new NotImplementedException();
        public Task<Session?> UpdateSessionAsync(Session session) => throw new NotImplementedException();
        public Task<bool> StartPlannedAsync(int userId, int sessionId, DateTime startedAt) => throw new NotImplementedException();
        public Task<bool> DeleteSessionAsync(int sessionId) => throw new NotImplementedException();
    }

    private sealed class FakeThemes : IThemeDatabaseService
    {
        public Task<List<Theme>> GetAllThemesAsync() => Task.FromResult(new List<Theme> { new() { Id = 1, Name = "test" } });
        public Task<Theme?> GetThemeByIdAsync(int themeId) => Task.FromResult<Theme?>(new Theme { Id = themeId, Name = "test" });
        public Task<Theme?> CreateThemeAsync(Theme theme) => throw new NotImplementedException();
        public Task<Theme?> UpdateThemeAsync(Theme theme) => throw new NotImplementedException();
        public Task<bool> DeleteThemeAsync(int themeId) => throw new NotImplementedException();
    }

    private sealed class FakeCatalog : IRewardDatabaseService
    {
        public Task<List<Reward>> GetAllRewardsAsync() => Task.FromResult(new List<Reward> { new() { Id = RewardCatalog.Pink, Name = "pink", Cost = 5 } });
        public Task<Reward?> GetRewardByIdAsync(int rewardId) => Task.FromResult<Reward?>(null);
        public Task<Reward?> CreateRewardAsync(Reward reward) => throw new NotImplementedException();
        public Task<Reward?> UpdateRewardAsync(Reward reward) => throw new NotImplementedException();
        public Task<bool> DeleteRewardAsync(int rewardId) => throw new NotImplementedException();
    }

    private sealed class FakeFeatures : IRewardShopService, ISessionCalendarService, ISessionHistoryService,
        ISessionPlanningService, ISessionStartService, ISessionRunService
    {
        private static readonly RewardShopState State = new(5, 0, "default", false, false);
        public string? LastKind { get; private set; }
        public Guid LastOperationId { get; private set; }
        public Task<RewardShopState?> GetAsync(int userId) => Task.FromResult<RewardShopState?>(State);
        public Task<RewardActionResult> PurchaseAsync(int userId, string kind, Guid operationId)
        {
            LastKind = kind;
            LastOperationId = operationId;
            return Task.FromResult(new RewardActionResult(RewardActionStatus.Success, State));
        }
        public Task<RewardActionResult> ActivateAsync(int userId, string theme) =>
            Task.FromResult(new RewardActionResult(RewardActionStatus.Success, State));
        public Task<List<CalendarSession>> GetSessionsAsync(int userId, DateTime from, DateTime to) => Task.FromResult(new List<CalendarSession>());
        public Task<List<SessionHistoryItem>> GetHistoryAsync(int userId) => Task.FromResult(new List<SessionHistoryItem>());
        public Task<SessionHistoryItem?> GetHistoryItemAsync(int userId, int sessionId) => Task.FromResult<SessionHistoryItem?>(null);
        public Task<SessionPlanningResult> PlanAsync(int userId, int duration, DateTimeOffset? plannedStartTime) =>
            Task.FromResult(new SessionPlanningResult(SessionPlanningStatus.Planned, new PlannedSessionResponse(1, duration, plannedStartTime!.Value.UtcDateTime)));
        public Task<SpontaneousSessionStartResult> StartSpontaneousAsync(int userId, int duration) =>
            Task.FromResult(new SpontaneousSessionStartResult(SpontaneousSessionStartStatus.Started, new StartedSessionResponse(1, duration, DateTime.UtcNow)));
        public Task<bool> StartPlannedAsync(int userId, int sessionId) => Task.FromResult(true);
        public Task<ActiveSession?> HeartbeatAsync(int userId, int sessionId) => throw new NotImplementedException();
        public Task<bool> CancelAsync(int userId, int sessionId) => throw new NotImplementedException();
        public Task<CompletionResult> CompleteAsync(int userId, int sessionId) => throw new NotImplementedException();
        public Task<RewardActionResult> ExtendBreakAsync(int userId, int sessionId, Guid operationId) => throw new NotImplementedException();
    }
}
