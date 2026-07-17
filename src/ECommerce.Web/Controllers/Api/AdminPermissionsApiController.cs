using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/admin/permissions")]
[Authorize(Policy = AuthConstants.Policies.AdminOnly)]
public sealed class AdminPermissionsApiController : ApiControllerBase
{
    private readonly IPermissionService _permissionService;

    public AdminPermissionsApiController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
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

        return ApiResponse<object?>.Ok(null, HttpContext.TraceIdentifier, "角色权限绑定成功");
    }
}
