using System.Text.Json;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using ECommerce.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/admin/permissions")]
[Authorize(Policy = AuthConstants.Policies.AdminOnly)]
public sealed class AdminPermissionsApiController : ApiControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly IOperationLogService _operationLogService;

    public AdminPermissionsApiController(
        IPermissionService permissionService,
        IOperationLogService operationLogService)
    {
        _permissionService = permissionService;
        _operationLogService = operationLogService;
    }

    [HttpGet("roles")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleDto>>>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _permissionService.GetRolesAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<RoleDto>>.Ok(roles, HttpContext.TraceIdentifier);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionDto>>>> GetPermissions(
        [FromQuery] string? keyword,
        CancellationToken cancellationToken)
    {
        var permissions = await _permissionService.GetPermissionsAsync(keyword, cancellationToken);
        return ApiResponse<IReadOnlyList<PermissionDto>>.Ok(permissions, HttpContext.TraceIdentifier);
    }

    [HttpGet("roles/{roleId:int}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RolePermissionDto>>>> GetRolePermissions(
        int roleId,
        CancellationToken cancellationToken)
    {
        var permissions = await _permissionService.GetRolePermissionsAsync(roleId, cancellationToken);
        return ApiResponse<IReadOnlyList<RolePermissionDto>>.Ok(permissions, HttpContext.TraceIdentifier);
    }

    [HttpPut("roles/{roleId:int}")]
    public async Task<ActionResult<ApiResponse<object?>>> BindRolePermissions(
        int roleId,
        [FromBody] BindRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        await _permissionService.BindRolePermissionsAsync(roleId, request.PermissionIds, cancellationToken);
        await _operationLogService.WriteAsync(new OperationLogRequest(
            User.GetUserId(),
            User.GetUsername(),
            "权限管理",
            "绑定角色权限",
            $"管理员为角色 {roleId} 绑定权限",
            GetClientIpAddress(),
            JsonSerializer.Serialize(request),
            1), cancellationToken);

        return ApiResponse<object?>.Ok(null, HttpContext.TraceIdentifier, "角色权限绑定成功");
    }
}
