using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Controllers;
using Tomatime.Api.DTOs;
using Tomatime.Api.Services;

namespace Tomatime.Api.Tests;

public class SessionStartControllerTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(24, false)]
    [InlineData(25, true)]
    [InlineData(300, true)]
    [InlineData(301, false)]
    public void RequestEnforcesDurationRange(int duration, bool expected)
    {
        var request = new StartSessionRequest { Duration = duration };
        var valid = Validator.TryValidateObject(request, new ValidationContext(request), [], true);
        Assert.Equal(expected, valid);
    }

    [Fact]
    public async Task StartingRequiresAUser()
    {
        var service = new FakeSessionStartService();
        var controller = new SessionStartController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        Assert.NotEmpty(typeof(SessionStartController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.IsType<UnauthorizedResult>(await controller.StartSpontaneous(new StartSessionRequest { Duration = 50 }));
        Assert.IsType<UnauthorizedResult>(await controller.StartPlanned(10));

        Assert.False(service.Called);
    }

    [Fact]
    public async Task ControllerMapsServiceResultsAndPassesAuthenticatedUser()
    {
        var service = new FakeSessionStartService();
        var controller = new SessionStartController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "7")], "test")),
                },
            },
        };
        service.Result = new(SpontaneousSessionStartStatus.InvalidDuration);
        Assert.IsType<BadRequestObjectResult>(await controller.StartSpontaneous(new StartSessionRequest { Duration = 26 }));
        Assert.Equal(7, service.UserId);
        Assert.Equal(26, service.Duration);

        service.Result = new(SpontaneousSessionStartStatus.UserNotFound);
        Assert.IsType<UnauthorizedResult>(await controller.StartSpontaneous(new StartSessionRequest { Duration = 50 }));
        var response = new StartedSessionResponse(4, 50, DateTime.UtcNow);
        service.Result = new(SpontaneousSessionStartStatus.Started, response);
        Assert.Equal(response, Assert.IsType<OkObjectResult>(await controller.StartSpontaneous(new StartSessionRequest { Duration = 50 })).Value);

        service.PlannedStarted = false;
        Assert.IsType<NotFoundObjectResult>(await controller.StartPlanned(42));
        Assert.Equal(42, service.SessionId);
        Assert.Equal(7, service.UserId);
        service.PlannedStarted = true;
        Assert.IsType<NoContentResult>(await controller.StartPlanned(42));
    }

    private sealed class FakeSessionStartService : ISessionStartService
    {
        public bool Called { get; private set; }
        public int UserId { get; private set; }
        public int Duration { get; private set; }
        public int SessionId { get; private set; }
        public bool PlannedStarted { get; set; }
        public SpontaneousSessionStartResult Result { get; set; } = new(SpontaneousSessionStartStatus.InvalidDuration);

        public Task<SpontaneousSessionStartResult> StartSpontaneousAsync(int userId, int duration)
        {
            Called = true;
            UserId = userId;
            Duration = duration;
            return Task.FromResult(Result);
        }

        public Task<bool> StartPlannedAsync(int userId, int sessionId)
        {
            Called = true;
            UserId = userId;
            SessionId = sessionId;
            return Task.FromResult(PlannedStarted);
        }
    }
}
