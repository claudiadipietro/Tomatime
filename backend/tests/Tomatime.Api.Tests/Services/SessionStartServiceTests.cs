using Tomatime.Api.Models;
using Tomatime.Api.Services;

namespace Tomatime.Api.Tests;

public class SessionStartServiceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(24)]
    [InlineData(26)]
    [InlineData(301)]
    [InlineData(325)]
    public async Task RejectsInvalidDurationWithoutCreatingSession(int duration)
    {
        var sessions = new FakeSessions();
        var service = new SessionStartService(new FakeUsers(), sessions);
        var result = await service.StartSpontaneousAsync(7, duration);
        Assert.Equal(SpontaneousSessionStartStatus.InvalidDuration, result.Status);
        Assert.Null(sessions.Created);
    }

    [Fact]
    public async Task MissingUserDoesNotCreateSession()
    {
        var sessions = new FakeSessions();
        var service = new SessionStartService(new FakeUsers { Exists = false }, sessions);
        var result = await service.StartSpontaneousAsync(7, 50);
        Assert.Equal(SpontaneousSessionStartStatus.UserNotFound, result.Status);
        Assert.Null(sessions.Created);
    }

    [Theory]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(300)]
    public async Task CreatesSpontaneousSessionForAuthenticatedUser(int duration)
    {
        var users = new FakeUsers();
        var sessions = new FakeSessions();
        var service = new SessionStartService(users, sessions);
        var before = DateTime.UtcNow;
        var result = await service.StartSpontaneousAsync(7, duration);
        var after = DateTime.UtcNow;
        Assert.Equal(SpontaneousSessionStartStatus.Started, result.Status);
        Assert.Equal(7, users.RequestedId);
        var session = Assert.IsType<Session>(sessions.Created);
        Assert.Equal(7, session.UserId);
        Assert.Equal(duration, session.Duration);
        Assert.Equal(SessionStatus.InProgress, session.Status);
        Assert.InRange(session.PlannedStartTime, before, after);
        Assert.Equal(session.PlannedStartTime, session.ActualStartTime);
        Assert.NotNull(result.Session);
        Assert.Equal(42, result.Session.Id);
        Assert.Equal(duration, result.Session.Duration);
    }

    [Fact]
    public async Task PlannedStartDelegatesUserAndSessionToAtomicDatabaseOperation()
    {
        var sessions = new FakeSessions { PlannedResult = true };
        var service = new SessionStartService(new FakeUsers(), sessions);
        var before = DateTime.UtcNow;
        Assert.True(await service.StartPlannedAsync(7, 99));
        Assert.Equal(7, sessions.UserId);
        Assert.Equal(99, sessions.SessionId);
        Assert.InRange(sessions.StartedAt, before, DateTime.UtcNow);
        sessions.PlannedResult = false;
        Assert.False(await service.StartPlannedAsync(7, 99));
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
        public bool PlannedResult { get; set; }
        public int UserId { get; private set; }
        public int SessionId { get; private set; }
        public DateTime StartedAt { get; private set; }
        public Task<Session?> CreateSessionAsync(Session session)
        {
            session.Id = 42;
            Created = session;
            return Task.FromResult<Session?>(session);
        }
        public Task<bool> StartPlannedAsync(int userId, int sessionId, DateTime startedAt)
        {
            UserId = userId;
            SessionId = sessionId;
            StartedAt = startedAt;
            return Task.FromResult(PlannedResult);
        }
        public Task<Session?> GetSessionByIdAsync(int sessionId) => throw new NotImplementedException();
        public Task<List<Session>> GetSessionsByUserIdAsync(int userId) => throw new NotImplementedException();
        public Task<Session?> UpdateSessionAsync(Session session) => throw new NotImplementedException();
        public Task<bool> DeleteSessionAsync(int sessionId) => throw new NotImplementedException();
    }
}
