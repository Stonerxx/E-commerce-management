using System.Security.Claims;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ECommerce.Web.Security;

public sealed class RefreshUserPrincipalCookieEvents : CookieAuthenticationEvents
{
    private static readonly TimeSpan RevalidationInterval = TimeSpan.FromMinutes(1);

    private readonly IUserRepository _userRepository;
    private readonly ILogger<RefreshUserPrincipalCookieEvents> _logger;

    public RefreshUserPrincipalCookieEvents(
        IUserRepository userRepository,
        ILogger<RefreshUserPrincipalCookieEvents> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        // Anonymous catalog pages do not use the authenticated identity for authorization.
        // Avoid turning every public page/API request into two Oracle round trips for users
        // who happen to have a login cookie.
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            return;
        }

        // The ticket is created from database-backed claims at sign-in. Recheck it at a
        // short interval instead of querying USER and USER_ROLE on every request.
        var issuedUtc = context.Properties.IssuedUtc;
        if (issuedUtc.HasValue && DateTimeOffset.UtcNow - issuedUtc.Value < RevalidationInterval)
        {
            return;
        }

        var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdValue, out var userId) || userId <= 0)
        {
            await RejectAsync(context);
            return;
        }

        try
        {
            var user = await _userRepository.GetByIdAsync(userId, context.HttpContext.RequestAborted);
            if (user is null || user.Status == 0)
            {
                await RejectAsync(context);
                return;
            }

            var currentRoles = context.Principal!
                .FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .OrderBy(role => role, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var databaseRoles = (await _userRepository.GetRoleNamesAsync(userId, context.HttpContext.RequestAborted))
                .OrderBy(role => role, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (currentRoles.SequenceEqual(databaseRoles, StringComparer.OrdinalIgnoreCase))
            {
                context.ShouldRenew = true;
                return;
            }

            var claims = context.Principal.Claims
                .Where(claim => claim.Type != ClaimTypes.Role)
                .ToList();
            claims.AddRange(databaseRoles.Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(claims, AuthConstants.AuthenticationScheme);
            context.ReplacePrincipal(new ClaimsPrincipal(identity));
            context.ShouldRenew = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh the authenticated user's roles.");
            await RejectAsync(context);
        }
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        return base.RedirectToLogin(context);
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        return base.RedirectToAccessDenied(context);
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(AuthConstants.AuthenticationScheme);
    }
}
