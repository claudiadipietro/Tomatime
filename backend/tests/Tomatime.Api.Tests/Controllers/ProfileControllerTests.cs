using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tomatime.Api.Controllers;
using Tomatime.Api.Services;

namespace Tomatime.Api.Tests;

public class ProfileControllerTests
{
    [Fact]
    public async Task UsesOnlyAuthenticatedUserForProfileOperations()
    {
        var service = new FakeProfile();
        var controller = new ProfileController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        Assert.NotEmpty(typeof(ProfileController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.IsType<UnauthorizedResult>(await controller.Get());
        Assert.IsType<UnauthorizedResult>(await controller.UpdateNotifications(new(true)));
        Assert.IsType<UnauthorizedResult>(await controller.ChangePassword(new("old-password", "new-password")));
        Assert.Equal(0, service.Calls);

        controller.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "7")], "test"));
        var profile = Assert.IsType<ProfileInfo>(Assert.IsType<OkObjectResult>(await controller.Get()).Value);
        Assert.Equal("Jane", profile.Name);
        Assert.Equal(7, service.UserId);
        Assert.IsType<NoContentResult>(await controller.UpdateNotifications(new(false)));
        Assert.False(service.Enabled);
        Assert.Equal(7, service.UserId);
        service.PasswordResult = PasswordChangeStatus.IncorrectPassword;
        Assert.IsType<BadRequestObjectResult>(await controller.ChangePassword(new("wrong-password", "new-password")));
        service.PasswordResult = PasswordChangeStatus.Success;
        Assert.IsType<NoContentResult>(await controller.ChangePassword(new("old-password", "new-password")));
        Assert.Equal(7, service.UserId);
    }

    private sealed class FakeProfile : IProfileService
    {
        public int Calls { get; private set; }
        public int UserId { get; private set; }
        public bool Enabled { get; private set; }
        public PasswordChangeStatus PasswordResult { get; set; }

        public Task<ProfileInfo?> GetAsync(int userId)
        {
            Calls++; UserId = userId;
            return Task.FromResult<ProfileInfo?>(new("Jane", "jane@example.com", 5, "default", true));
        }
        public Task<bool> UpdateNotificationsAsync(int userId, bool enabled)
        {
            Calls++; UserId = userId; Enabled = enabled;
            return Task.FromResult(true);
        }
        public Task<PasswordChangeStatus> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            Calls++; UserId = userId;
            return Task.FromResult(PasswordResult);
        }
    }
}
