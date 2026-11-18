using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Controllers;
using Tomatime.Api.DTOs;
using Tomatime.Api.Models;
using Tomatime.Api.Services;

namespace Tomatime.Api.Tests;

public class SessionRunControllerTests
{
    [Fact]
    public async Task RequiresAuthenticationAndUsesAuthenticatedUserForTransitions()
    {
        var service = new FakeRuns();
        var controller = new SessionController(null!, null!, null!, null!, null!, service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        Assert.NotEmpty(typeof(SessionController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.IsType<UnauthorizedResult>(await controller.Heartbeat(9));
        Assert.IsType<UnauthorizedResult>(await controller.Cancel(9));
        Assert.IsType<UnauthorizedResult>(await controller.Complete(9));
        Assert.Equal(0, service.Calls);

        controller.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "7")], "test"));
        Assert.IsType<NoContentResult>(await controller.Cancel(9));
        Assert.Equal(7, service.UserId);
        Assert.Equal(9, service.SessionId);
        service.Result = new(CompletionStatus.TooEarly);
        Assert.IsType<ConflictObjectResult>(await controller.Complete(9));
        service.Result = new(CompletionStatus.NotFound);
        Assert.IsType<NotFoundResult>(await controller.Complete(9));
        service.Result = new(CompletionStatus.Completed, new SessionCompletion(4));
        Assert.Equal(4, Assert.IsType<SessionCompletion>(
            Assert.IsType<OkObjectResult>(await controller.Complete(9)).Value).PomosEarned);
    }

    private sealed class FakeRuns : ISessionRunService
    {
        public int Calls { get; private set; }
        public int UserId { get; private set; }
        public int SessionId { get; private set; }
        public CompletionResult Result { get; set; } = new(CompletionStatus.NotFound);

        public Task<ActiveSession?> HeartbeatAsync(int userId, int sessionId) =>
            Task.FromResult<ActiveSession?>(null);

        public Task<bool> CancelAsync(int userId, int sessionId)
        {
            Calls++;
            UserId = userId;
            SessionId = sessionId;
            return Task.FromResult(true);
        }

        public Task<CompletionResult> CompleteAsync(int userId, int sessionId)
        {
            Calls++;
            UserId = userId;
            SessionId = sessionId;
            return Task.FromResult(Result);
        }

        public Task<RewardActionResult> ExtendBreakAsync(int userId, int sessionId, Guid operationId) =>
            Task.FromResult(new RewardActionResult(RewardActionStatus.InvalidOperation));
    }
}
