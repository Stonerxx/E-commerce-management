using ECommerce.Shared.Contracts;
using ECommerce.Shared.Errors;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.Web.Controllers.Api;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected long GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !long.TryParse(claim, out var userId))
        {
            throw new UnauthorizedAccessException("用户未登录或身份信息无效");
        }

        return userId;
    }

    protected string GetCurrentUserName()
    {
        return User.Identity?.Name ?? "未知用户";
    }

    protected string GetClientIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var firstForwardedIp = forwardedFor
                .ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstForwardedIp))
            {
                return firstForwardedIp;
            }
        }

        if (Request.Headers.TryGetValue("X-Real-IP", out var realIp)
            && !string.IsNullOrWhiteSpace(realIp.ToString()))
        {
            return realIp.ToString();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    protected ActionResult<ApiResponse<T>> NotReady<T>(string message)
    {
        var response = ApiResponse<T>.Fail(
            ErrorCodes.NotImplemented,
            message,
            HttpContext.TraceIdentifier);

        return StatusCode(StatusCodes.Status501NotImplemented, response);
    }
}
