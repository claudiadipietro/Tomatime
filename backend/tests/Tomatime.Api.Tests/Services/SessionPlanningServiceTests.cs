using Tomatime.Api.Models;
using Tomatime.Api.Services;

namespace Tomatime.Api.Tests;

public class SessionPlanningServiceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(26)]
    [InlineData(325)]
    public async Task RejectsInvalidDurations(int duration)
    {
        var sessions = new FakeSessions();
        var service = new SessionPlanningService(new FakeUsers(), sessions);
        Assert.Equal(SessionPlanningStatus.InvalidDuration,
            (await service.PlanAsync(7, duration, DateTimeOffset.UtcNow.AddDays(1))).Status);
        Assert.Null(sessions.Created);
    }

    [Fact]
    public async Task RejectsMissingOrPastDatesAndUnknownUsers()
    {
        var sessions = new FakeSessions();
        var users = new FakeUsers();
        var service = new SessionPlanningService(users, sessions);
        Assert.Equal(SessionPlanningStatus.InvalidStartTime, (await service.PlanAsync(7, 50, null)).Status);
        Assert.Equal(SessionPlanningStatus.InvalidStartTime, (await service.PlanAsync(7, 50, DateTimeOffset.UtcNow.AddMinutes(-1))).Status);
        users.Exists = false;
        Assert.Equal(SessionPlanningStatus.UserNotFound, (await service.PlanAsync(7, 50, DateTimeOffset.UtcNow.AddDays(1))).Status);
        Assert.Null(sessions.Created);
    }

    [Fact]
    public async Task SavesFutureSessionInUtcForAuthenticatedUserWithoutStarting()
    {
        var users = new FakeUsers();
        var sessions = new FakeSessions();
        var service = new SessionPlanningService(users, sessions);
        var start = DateTimeOffset.UtcNow.AddDays(1).ToOffset(TimeSpan.FromHours(2));
        var result = await service.PlanAsync(7, 75, start);
        Assert.Equal(SessionPlanningStatus.Planned, result.Status);
        Assert.Equal(7, users.RequestedId);
        var session = Assert.IsType<Session>(sessions.Created);
        Assert.Equal(7, session.UserId);
        Assert.Equal(75, session.Duration);
        Assert.Equal(start.UtcDateTime, session.PlannedStartTime);
        Assert.Equal(DateTimeKind.Utc, session.PlannedStartTime.Kind);
        Assert.Equal(SessionStatus.Planned, session.Status);
        Assert.Null(session.ActualStartTime);
        Assert.NotNull(result.Session);
        Assert.Equal(42, result.Session.Id);
    }

    private sealed class FakeUsers : IUserDatabaseService
    {
        public bool Exists { get; set; } = true;
        public int RequestedId { get; private set; }
        public Task<User?> GetUserByIdAsync(int userId)
        {
            RequestedId = userId;
            return Task.FromResult(Exists ? new User { Id = userId } : null);
        }
        public Task<User?> GetUserByEmailAsync(string email) => throw new NotImplementedException();
        public Task<User?> CreateUserAsync(User user) => throw new NotImplementedException();
        public Task<User?> UpdateUserAsync(User user) => throw new NotImplementedException();
        public Task<bool> DeleteUserAsync(int userId) => throw new NotImplementedException();
    }

    private sealed class FakeSessions : ISessionDatabaseService
    {
        public Session? Created { get; private set; }
        public Task<Session?> CreateSessionAsync(Session session)
        {
            session.Id = 42;
            Created = session;
            return Task.FromResult<Session?>(session);
        }
        public Task<Session?> GetSessionByIdAsync(int sessionId) => throw new NotImplementedException();
        public Task<List<Session>> GetSessionsByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<Session?> UpdateSessionAsync(Session session) => throw new NotImplementedException();
        public Task<bool> StartPlannedAsync(int userId, int sessionId, DateTime startedAt) => throw new NotImplementedException();
        public Task<bool> DeleteSessionAsync(int sessionId) => throw new NotImplementedException();
    }
}
