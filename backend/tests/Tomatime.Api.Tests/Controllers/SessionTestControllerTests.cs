using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Controllers;
using Tomatime.Api.Services;

namespace Tomatime.Api.Tests;

public class SessionTestControllerTests
{
    [Fact]
    public async Task DisabledClockRefusesAdvancementBeforeAccessingTheDatabase()
    {
        var controller = new SessionTestController(null!, new SessionTestClock(false), null!);
        Assert.IsType<NotFoundResult>(await controller.Advance(1, new(1500)));
    }

    [Fact]
    public async Task EnabledClockRequiresAnAuthenticatedUser()
    {
        var controller = new SessionTestController(null!, new SessionTestClock(true), null!)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
        };
        Assert.IsType<UnauthorizedResult>(await controller.Advance(1, new(1500)));
    }
}
