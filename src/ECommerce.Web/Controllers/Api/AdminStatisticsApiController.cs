using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> DashboardSummary(CancellationToken cancellationToken)
    {
        var result = await _statisticsService.GetDashboardSummaryAsync(cancellationToken);
        return ApiResponse<DashboardSummaryDto>.Ok(result, HttpContext.TraceIdentifier);
    }

    [HttpGet("statistics/orders")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<OrderStatisticsDto>>> OrderStatistics(
        [FromQuery] StatisticsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _statisticsService.GetOrderStatisticsAsync(query, cancellationToken);
        return ApiResponse<OrderStatisticsDto>.Ok(result, HttpContext.TraceIdentifier);
    }

    [HttpGet("statistics/top-products")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TopProductDto>>>> TopProducts(
        [FromQuery] StatisticsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _statisticsService.GetTopProductsAsync(query, cancellationToken);
        return ApiResponse<IReadOnlyList<TopProductDto>>.Ok(result, HttpContext.TraceIdentifier);
    }

    [HttpGet("exports/orders")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<IActionResult> ExportOrders(
        [FromQuery] AdminOrderQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _exportService.ExportOrdersAsync(query, cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet("exports/inventory")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<IActionResult> ExportInventory(
        [FromQuery] InventoryLogQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _exportService.ExportInventoryAsync(query, cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }
}
