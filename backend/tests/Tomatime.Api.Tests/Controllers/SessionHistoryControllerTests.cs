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
        var controller = new SessionController(null!, null!, service, null!, null!, null!)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        Assert.NotEmpty(typeof(SessionController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.IsType<UnauthorizedResult>(await controller.GetHistory());
        Assert.IsType<UnauthorizedResult>(await controller.GetHistoryItem(42));
        Assert.Null(service.RequestedUser);
        controller.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "7")], "test"));
        Assert.IsType<List<SessionHistoryItem>>(Assert.IsType<OkObjectResult>(await controller.GetHistory()).Value);
        Assert.Equal(7, service.RequestedUser);
        Assert.IsType<NotFoundResult>(await controller.GetHistoryItem(42));
        Assert.Equal(7, service.RequestedUser);
        Assert.Equal(42, service.RequestedSession);
        service.Item = new SessionHistoryItem(42, 50, "Completed", DateTime.UtcNow, null, 4);
        Assert.Equal(service.Item, Assert.IsType<OkObjectResult>(await controller.GetHistoryItem(42)).Value);
    }

    private sealed class FakeHistory : ISessionHistoryService
    {
        public int? RequestedUser { get; private set; }
        public int? RequestedSession { get; private set; }
        public SessionHistoryItem? Item { get; set; }
        public Task<SessionHistoryItem?> GetHistoryItemAsync(int userId, int sessionId)
        {
            RequestedUser = userId;
            RequestedSession = sessionId;
            return Task.FromResult(Item);
        }
        public Task<List<SessionHistoryItem>> GetHistoryAsync(int userId)
        {
            RequestedUser = userId;
            return Task.FromResult(new List<SessionHistoryItem>());
        }
    }
}
