using ECommerce.Shared.Contracts;
using ECommerce.Shared.Errors;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.Web.Controllers.Api;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// 获取当前登录用户 ID
    /// </summary>
    protected long GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !long.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("用户未登录或身份信息无效");
        return userId;
    }

    /// <summary>
    /// 获取当前登录用户名
    /// </summary>
    protected string GetCurrentUserName()
    {
        return User.Identity?.Name ?? "未知用户";
    }

    /// <summary>
    /// 返回 501 未实现（用于骨架占位）
    /// </summary>
    protected ActionResult<ApiResponse<T>> NotReady<T>(string message)
    {
        var response = ApiResponse<T>.Fail(
            ErrorCodes.NotImplemented,
            message,
            HttpContext.TraceIdentifier);

        return StatusCode(StatusCodes.Status501NotImplemented, response);
    }
}
