using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Controllers;
using Tomatime.Api.DTOs;
using Tomatime.Api.Services;

namespace Tomatime.Api.Tests;

public class SessionHistoryControllerTests
{
    [Fact]
    public async Task HistoryRequiresUserAndUsesClaims()
    {
        var service = new FakeHistory();
        var controller = new SessionHistoryController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        Assert.NotEmpty(typeof(SessionHistoryController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.IsType<UnauthorizedResult>(await controller.Get());
        Assert.Null(service.RequestedUser);
        controller.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "7")], "test"));
        Assert.IsType<List<SessionHistoryItem>>(Assert.IsType<OkObjectResult>(await controller.Get()).Value);
        Assert.Equal(7, service.RequestedUser);
    }

    private sealed class FakeHistory : ISessionHistoryService
    {
        public int? RequestedUser { get; private set; }
        public Task<List<SessionHistoryItem>> GetHistoryAsync(int userId)
        {
            RequestedUser = userId;
            return Task.FromResult(new List<SessionHistoryItem>());
        }
    }
}
