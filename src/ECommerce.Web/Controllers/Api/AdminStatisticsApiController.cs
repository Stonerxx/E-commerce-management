using ECommerce.Application.DTOs;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ECommerce.Application.Services;
using System.Threading.Tasks;
using System.Threading;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/admin")]
public sealed class AdminStatisticsApiController : ApiControllerBase
{

    private readonly IStatisticsService _statisticsService;
    private readonly IExportService _exportService;

    public AdminStatisticsApiController(IStatisticsService statisticsService, IExportService exportService)
    {
        _statisticsService = statisticsService;
        _exportService = exportService;
    }

    [HttpGet("dashboard/summary")]
    [Authorize(Policy = AuthConstants.Policies.ServiceOrAdmin)]
    // [AllowAnonymous] // ≤‚ ‘”√
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> DashboardSummaryAsync()
    {
        var result = await _statisticsService.GetDashboardSummaryAsync();
        return ApiResponse<DashboardSummaryDto>.Ok(result);
    }

    [HttpGet("statistics/orders")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    // [AllowAnonymous] // ≤‚ ‘”√
    public async Task<ActionResult<ApiResponse<OrderStatisticsDto>>> OrderStatisticsAsync([FromQuery] StatisticsQuery query)
    {
        var result = await _statisticsService.GetOrderStatisticsAsync(query);
        return ApiResponse<OrderStatisticsDto>.Ok(result);
    }

    [HttpGet("statistics/top-products")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    // [AllowAnonymous] // ≤‚ ‘”√
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TopProductDto>>>> TopProductsAsync([FromQuery] StatisticsQuery query)
    {
        var result = await _statisticsService.GetTopProductsAsync(query);
        return ApiResponse<IReadOnlyList<TopProductDto>>.Ok(result);
    }

    [HttpGet("exports/orders")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    // [AllowAnonymous] // ≤‚ ‘”√
    public async Task<ActionResult<ApiResponse<FileExportDto>>> ExportOrdersAsync([FromQuery] AdminOrderQuery query)
    {
        var result = await _exportService.ExportOrdersAsync(query);
        return ApiResponse<FileExportDto>.Ok(result);
    }

    [HttpGet("exports/inventory")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    // [AllowAnonymous] // ≤‚ ‘”√
    public async Task<ActionResult<ApiResponse<FileExportDto>>> ExportInventoryAsync([FromQuery] InventoryLogQuery query)
    {
        var result = await _exportService.ExportInventoryAsync(query);
        return ApiResponse<FileExportDto>.Ok(result);
    }
}
