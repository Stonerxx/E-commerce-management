using System.Security.Claims;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests;

public sealed class AccountControllerTests
{
    [Fact]
    public void Login_redirects_authenticated_admin_to_admin_workspace()
    {
        var controller = CreateController(AuthConstants.Roles.Admin);

        var result = Assert.IsType<RedirectResult>(controller.Login());

        Assert.Equal("/admin", result.Url);
    }

    [Fact]
    public void Register_redirects_authenticated_customer_to_storefront()
    {
        var controller = CreateController(AuthConstants.Roles.User);

        var result = Assert.IsType<RedirectResult>(controller.Register());

        Assert.Equal("/", result.Url);
    }

    [Fact]
    public void Login_keeps_anonymous_user_on_login_page()
    {
        var controller = CreateController();

        Assert.IsType<ViewResult>(controller.Login());
    }

    private static AccountController CreateController(string? role = null)
    {
        var controller = new AccountController(
            Mock.Of<IAuthService>(),
            Mock.Of<ILogger<AccountController>>());

        var claims = role == null
            ? Array.Empty<Claim>()
            : new[] { new Claim(ClaimTypes.Name, "demo"), new Claim(ClaimTypes.Role, role) };
        var identity = role == null
            ? new ClaimsIdentity()
            : new ClaimsIdentity(claims, AuthConstants.AuthenticationScheme);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        return controller;
    }
}
