using System.Security.Claims;
using ECommerce.Application.Services;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ECommerce.Web.Filters;

/// <summary>
/// 全局 RBAC 权限过滤器：先由登录和角色 Policy 做基础校验，再根据 PERMISSION 和 ROLE_PERMISSION 表做动态权限校验。
/// </summary>
public sealed class RbacPermissionFilter : IAsyncAuthorizationFilter
{
    private readonly IPermissionService _permissionService;

    public RbacPermissionFilter(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any())
        {
            return;
        }

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var roles = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        var path = context.HttpContext.Request.Path.Value ?? "/";
        var method = context.HttpContext.Request.Method;

        if (await _permissionService.CanAccessAsync(roles, path, method, context.HttpContext.RequestAborted))
        {
            return;
        }

        context.Result = new ObjectResult(
            ApiResponse<object?>.Fail(ErrorCodes.AuthForbidden, "当前角色没有访问该资源的权限", context.HttpContext.TraceIdentifier))
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}
