using System.Security.Claims;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Errors;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<ApiResponse<T>> NotReady<T>(string message)
    {
        var response = ApiResponse<T>.Fail(
            ErrorCodes.NotImplemented,
            message,
            HttpContext.TraceIdentifier);

        return StatusCode(StatusCodes.Status501NotImplemented, response);
    }

    protected long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            throw new InvalidOperationException("用户未登录或用户ID无效");
        }
        return userId;
    }

    protected string GetCurrentUserName()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
    }
}