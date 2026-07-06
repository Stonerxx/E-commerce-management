using ECommerce.Application.DTOs;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/admin")]
[Authorize(Policy = AuthConstants.Policies.AdminOnly)]
public sealed class AdminStatisticsApiController : ApiControllerBase
{
    [HttpGet("dashboard/summary")]
    public ActionResult<ApiResponse<DashboardSummaryDto>> DashboardSummary()
    {
        return NotReady<DashboardSummaryDto>("Dashboard summary endpoint is defined and awaiting implementation.");
    }

    [HttpGet("statistics/orders")]
    public ActionResult<ApiResponse<OrderStatisticsDto>> OrderStatistics([FromQuery] StatisticsQuery query)
    {
        return NotReady<OrderStatisticsDto>("Order statistics endpoint is defined and awaiting implementation.");
    }

    [HttpGet("statistics/top-products")]
    public ActionResult<ApiResponse<IReadOnlyList<TopProductDto>>> TopProducts([FromQuery] StatisticsQuery query)
    {
        return NotReady<IReadOnlyList<TopProductDto>>("Top product statistics endpoint is defined and awaiting implementation.");
    }

    [HttpGet("exports/orders")]
    public ActionResult<ApiResponse<FileExportDto>> ExportOrders([FromQuery] AdminOrderQuery query)
    {
        return NotReady<FileExportDto>("Order export endpoint is defined and awaiting implementation.");
    }

    [HttpGet("exports/inventory")]
    public ActionResult<ApiResponse<FileExportDto>> ExportInventory([FromQuery] InventoryLogQuery query)
    {
        return NotReady<FileExportDto>("Inventory export endpoint is defined and awaiting implementation.");
    }
}
