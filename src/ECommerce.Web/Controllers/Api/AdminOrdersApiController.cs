using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/admin/orders")]
[Authorize(Policy = AuthConstants.Policies.ServiceOrAdmin)]
public sealed class AdminOrdersApiController : ApiControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<AdminOrdersApiController> _logger;

    public AdminOrdersApiController(IOrderService orderService, ILogger<AdminOrdersApiController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// 后台订单列表（分页，支持多条件筛选）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderListItemDto>>>> Search(
        [FromQuery] AdminOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.SearchAdminAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResult<OrderListItemDto>>.Ok(result));
    }

    /// <summary>
    /// 后台订单详情
    /// </summary>
    [HttpGet("{orderId:long}")]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> Detail(
        long orderId,
        CancellationToken cancellationToken = default)
    {
        var detail = await _orderService.GetAdminDetailAsync(orderId, cancellationToken);
        return Ok(ApiResponse<OrderDetailDto>.Ok(detail));
    }

    /// <summary>
    /// 客服或管理员取消待支付订单
    /// </summary>
    [HttpPost("{orderId:long}/cancel")]
    [Authorize(Policy = AuthConstants.Policies.ServiceOrAdmin)]
    public async Task<ActionResult<ApiResponse<object>>> AdminCancel(
        long orderId,
        [FromBody] AdminCancelOrderRequest? request,
        CancellationToken cancellationToken = default)
    {
        var operatorId = GetCurrentUserId();
        var operatorName = GetCurrentUserName();
        var ipAddress = GetClientIpAddress();

        var order = await _orderService.GetAdminDetailAsync(orderId, cancellationToken);

        await _orderService.CancelAsync(
            order.UserId,    // 订单归属者
            orderId,
            operatorId,      // 实际后台操作人
            operatorName,    // 实际后台操作人姓名
            ipAddress,
            request?.Reason ?? "后台取消",
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, message: "订单已取消"));
    }
}

/// <summary>
/// 后台取消订单请求
/// </summary>
public sealed record AdminCancelOrderRequest(string? Reason);
