using ECommerce.Application.DTOs;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/admin/users")]
[Authorize(Policy = AuthConstants.Policies.AdminOnly)]
public sealed class AdminUsersApiController : ApiControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<PagedResult<UserDto>>> Search([FromQuery] UserQuery query)
    {
        return NotReady<PagedResult<UserDto>>("Admin user search endpoint is defined and awaiting implementation.");
    }

    [HttpPut("{userId:long}/status")]
    public ActionResult<ApiResponse<object?>> SetStatus(long userId, [FromBody] StatusUpdateRequest request)
    {
        return NotReady<object?>("User status endpoint is defined and awaiting implementation.");
    }

    [HttpPut("{userId:long}/roles")]
    public ActionResult<ApiResponse<object?>> AssignRoles(long userId, [FromBody] AssignRolesRequest request)
    {
        return NotReady<object?>("User role assignment endpoint is defined and awaiting implementation.");
    }
}
