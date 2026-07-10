using System.Security.Claims;

namespace ECommerce.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static long GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(value, out var userId) ? userId : 0;
    }

    public static string GetUsername(this ClaimsPrincipal user)
    {
        return user.Identity?.Name ?? string.Empty;
    }
}
