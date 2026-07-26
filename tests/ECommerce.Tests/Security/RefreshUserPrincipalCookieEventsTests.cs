using System.Security.Claims;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Constants;
using ECommerce.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Security;

public sealed class RefreshUserPrincipalCookieEventsTests
{
    [Fact]
    public async Task ValidatePrincipal_AnonymousEndpoint_ShouldSkipDatabaseLookup()
    {
        var repository = new Mock<IUserRepository>(MockBehavior.Strict);
        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AllowAnonymousAttribute()),
            "anonymous"));
        var context = CreateContext(httpContext, DateTimeOffset.UtcNow.AddMinutes(-5));
        var events = CreateEvents(repository);

        await events.ValidatePrincipal(context);

        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidatePrincipal_FreshTicket_ShouldSkipDatabaseLookup()
    {
        var repository = new Mock<IUserRepository>(MockBehavior.Strict);
        var context = CreateContext(new DefaultHttpContext(), DateTimeOffset.UtcNow);
        var events = CreateEvents(repository);

        await events.ValidatePrincipal(context);

        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidatePrincipal_ExpiredValidationWindow_ShouldRefreshAndRenewTicket()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(item => item.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Username = "demo_user", Status = 1 });
        repository.Setup(item => item.GetRoleNamesAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { AuthConstants.Roles.User });
        var context = CreateContext(new DefaultHttpContext(), DateTimeOffset.UtcNow.AddMinutes(-2));
        var events = CreateEvents(repository);

        await events.ValidatePrincipal(context);

        Assert.True(context.ShouldRenew);
        repository.Verify(item => item.GetByIdAsync(7, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(item => item.GetRoleNamesAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static RefreshUserPrincipalCookieEvents CreateEvents(Mock<IUserRepository> repository) =>
        new(repository.Object, new Mock<ILogger<RefreshUserPrincipalCookieEvents>>().Object);

    private static CookieValidatePrincipalContext CreateContext(
        HttpContext httpContext,
        DateTimeOffset issuedUtc)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "7"),
                new Claim(ClaimTypes.Name, "demo_user"),
                new Claim(ClaimTypes.Role, AuthConstants.Roles.User)
            ],
            AuthConstants.AuthenticationScheme);
        var properties = new AuthenticationProperties { IssuedUtc = issuedUtc };
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(identity),
            properties,
            AuthConstants.AuthenticationScheme);
        var scheme = new AuthenticationScheme(
            AuthConstants.AuthenticationScheme,
            null,
            typeof(CookieAuthenticationHandler));

        return new CookieValidatePrincipalContext(
            httpContext,
            scheme,
            new CookieAuthenticationOptions(),
            ticket);
    }
}
