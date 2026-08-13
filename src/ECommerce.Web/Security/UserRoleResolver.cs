using System.Security.Claims;
using ECommerce.Shared.Constants;

namespace ECommerce.Web.Security;

public static class UserRoleResolver
{
    public static string? ResolvePrimary(ClaimsPrincipal user)
    {
        return ResolvePrimary(user.FindAll(ClaimTypes.Role).Select(claim => claim.Value));
    }

    public static string? ResolvePrimary(IEnumerable<string> roles)
    {
        var roleSet = roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (roleSet.Contains(AuthConstants.Roles.Admin)) return AuthConstants.Roles.Admin;
        if (roleSet.Contains(AuthConstants.Roles.Service)) return AuthConstants.Roles.Service;
        if (roleSet.Contains(AuthConstants.Roles.User)) return AuthConstants.Roles.User;
        return null;
    }

    public static string GetLandingPath(IEnumerable<string> roles)
    {
        return ResolvePrimary(roles) is AuthConstants.Roles.Admin or AuthConstants.Roles.Service
            ? "/admin"
            : "/";
    }

    public static string GetLandingPath(ClaimsPrincipal user)
    {
        return ResolvePrimary(user) is AuthConstants.Roles.Admin or AuthConstants.Roles.Service
            ? "/admin"
            : "/";
    }
}
