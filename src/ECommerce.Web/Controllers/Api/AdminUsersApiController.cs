using System.Text.Json;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using ECommerce.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/admin/users")]
[Authorize(Policy = AuthConstants.Policies.AdminOnly)]
public sealed class AdminUsersApiController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly IOperationLogService _operationLogService;

    public AdminUsersApiController(IUserService userService, IOperationLogService operationLogService)
    {
        _userService = userService;
        _operationLogService = operationLogService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> Search(
        [FromQuery] UserQuery query,
        CancellationToken cancellationToken)
    {
        var users = await _userService.SearchUsersAsync(query, cancellationToken);
        return ApiResponse<PagedResult<UserDto>>.Ok(users, HttpContext.TraceIdentifier);
    }

    [HttpPut("{userId:long}/status")]
    public async Task<ActionResult<ApiResponse<object?>>> SetStatus(
        long userId,
        [FromBody] StatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.SetUserStatusAsync(userId, request.Status, User.GetUserId(), cancellationToken);
        await WriteLogAsync(
            "用户管理",
            "修改用户状态",
            $"管理员修改用户 {userId} 状态为 {request.Status}",
            request,
            cancellationToken);

        return ApiResponse<object?>.Ok(null, HttpContext.TraceIdentifier, "用户状态修改成功");
    }

    [HttpPut("{userId:long}/roles")]
    public async Task<ActionResult<ApiResponse<object?>>> AssignRoles(
        long userId,
        [FromBody] AssignRolesRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.AssignRolesAsync(userId, request.RoleIds, User.GetUserId(), cancellationToken);
        await WriteLogAsync(
            "用户管理",
            "分配用户角色",
            $"管理员为用户 {userId} 分配角色",
            request,
            cancellationToken);

        return ApiResponse<object?>.Ok(null, HttpContext.TraceIdentifier, "用户角色分配成功");
    }

    private async Task WriteLogAsync(string module, string action, string description, object request, CancellationToken cancellationToken)
    {
        await _operationLogService.WriteAsync(new OperationLogRequest(
            User.GetUserId(),
            User.GetUsername(),
            module,
            action,
            description,
            GetClientIpAddress(),
            JsonSerializer.Serialize(request),
            1), cancellationToken);
    }
}
