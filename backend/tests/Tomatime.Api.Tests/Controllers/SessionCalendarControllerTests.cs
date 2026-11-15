using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Controllers;
using Tomatime.Api.DTOs;
using Tomatime.Api.Services;

namespace Tomatime.Api.Tests;

public class SessionCalendarControllerTests
{
    [Fact]
    public async Task ValidatesRangeAndUsesAuthenticatedUserWithUtcBounds()
    {
        var service = new FakeCalendar();
        var controller = new SessionCalendarController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        Assert.NotEmpty(typeof(SessionCalendarController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
        var from = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.FromHours(1));
        var to = from.AddDays(31);
        Assert.IsType<UnauthorizedResult>(await controller.Get(from, to));
        controller.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "7")], "test"));
        Assert.IsType<BadRequestObjectResult>(await controller.Get(null, to));
        Assert.IsType<BadRequestObjectResult>(await controller.Get(from, from));
        Assert.IsType<BadRequestObjectResult>(await controller.Get(from, from.AddDays(46)));
        Assert.Equal(0, service.Calls);
        Assert.IsType<OkObjectResult>(await controller.Get(from, to));
        Assert.Equal(7, service.UserId);
        Assert.Equal(from.UtcDateTime, service.From);
        Assert.Equal(to.UtcDateTime, service.To);
    }

    private sealed class FakeCalendar : ISessionCalendarService
    {
        public int Calls { get; private set; }
        public int UserId { get; private set; }
        public DateTime From { get; private set; }
        public DateTime To { get; private set; }
        public Task<List<CalendarSession>> GetSessionsAsync(int userId, DateTime from, DateTime to)
        {
            Calls++;
            UserId = userId;
            From = from;
            To = to;
            return Task.FromResult(new List<CalendarSession>());
        }
    }
}
