using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Controllers;
using Tomatime.Api.DTOs;
using Tomatime.Api.Models;
using Tomatime.Api.Services;

namespace Tomatime.Api.Tests;

public class DashboardControllerTests
{
    [Fact]
    public async Task DashboardUsesAuthenticatedUserAndSummarizesCompletedAndUpcomingSessions()
    {
        var users = new FakeUsers();
        var sessions = new FakeSessions();
        var controller = new DashboardController(users, sessions)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, "7")], "test")),
                },
            },
        };

        var response = Assert.IsType<DashboardResponse>(Assert.IsType<OkObjectResult>(await controller.Get()).Value);
        Assert.Equal(7, users.RequestedId);
        Assert.Equal(7, sessions.RequestedId);
        Assert.Equal(24, response.PomoBalance);
        Assert.Equal(2, response.CompletedSessions);
        Assert.Equal(75, response.FocusMinutes);
        Assert.NotNull(response.NextSession);
        Assert.Equal(30, response.NextSession.Duration);
        Assert.NotEmpty(typeof(DashboardController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
    }

    [Fact]
    public async Task DashboardRejectsMissingUserId()
    {
        var controller = new DashboardController(new FakeUsers(), new FakeSessions())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        Assert.IsType<UnauthorizedResult>(await controller.Get());
    }

    private sealed class FakeUsers : IUserDatabaseService
    {
        public int RequestedId { get; private set; }
        public Task<User?> GetUserByIdAsync(int userId)
        {
            RequestedId = userId;
            return Task.FromResult<User?>(new User { Id = userId, PomoBalance = 24 });
        }
        public Task<User?> GetUserByEmailAsync(string email) => throw new NotImplementedException();
        public Task<User?> CreateUserAsync(User user) => throw new NotImplementedException();
        public Task<User?> UpdateUserAsync(User user) => throw new NotImplementedException();
        public Task<bool> DeleteUserAsync(int userId) => throw new NotImplementedException();
    }

    private sealed class FakeSessions : ISessionDatabaseService
    {
        public int RequestedId { get; private set; }
        public Task<List<Session>> GetSessionsByUserIdAsync(int userId)
        {
            RequestedId = userId;
            var now = DateTime.UtcNow;
            return Task.FromResult(new List<Session>
            {
                new() { Status = SessionStatus.Planned, PlannedStartTime = now.AddHours(2), Duration = 60 },
                new() { Status = SessionStatus.Planned, PlannedStartTime = now.AddHours(1), Duration = 30 },
                new() { Status = SessionStatus.Planned, PlannedStartTime = now.AddHours(-1), Duration = 10 },
                new() { Status = SessionStatus.Completed, Duration = 50 },
                new() { Status = SessionStatus.Completed, Duration = 25 },
                new() { Status = SessionStatus.Cancelled, Duration = 100 },
            });
        }
        public Task<Session?> GetSessionByIdAsync(int sessionId) => throw new NotImplementedException();
        public Task<Session?> CreateSessionAsync(Session session) => throw new NotImplementedException();
        public Task<Session?> UpdateSessionAsync(Session session) => throw new NotImplementedException();
        public Task<bool> DeleteSessionAsync(int sessionId) => throw new NotImplementedException();
    }
}
