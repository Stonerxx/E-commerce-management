using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/admin/operation-logs")]
[Authorize(Policy = AuthConstants.Policies.AdminOnly)]
public sealed class AdminOperationLogsApiController : ApiControllerBase
{
    private readonly IOperationLogService _operationLogService;

    public AdminOperationLogsApiController(IOperationLogService operationLogService)
    {
        _operationLogService = operationLogService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<OperationLogDto>>>> Search(
        [FromQuery] OperationLogQuery query,
        CancellationToken cancellationToken)
    {
        var logs = await _operationLogService.SearchAsync(query, cancellationToken);
        return ApiResponse<PagedResult<OperationLogDto>>.Ok(logs, HttpContext.TraceIdentifier);
    }
}
