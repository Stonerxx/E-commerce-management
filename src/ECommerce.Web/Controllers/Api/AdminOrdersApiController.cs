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
    /// 后台强制取消订单（仅管理员）
    /// </summary>
    [HttpPost("{orderId:long}/cancel")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<object>>> AdminCancel(
        long orderId,
        [FromBody] AdminCancelOrderRequest? request,
        CancellationToken cancellationToken = default)
    {
        var operatorId = GetCurrentUserId();
        var operatorName = GetCurrentUserName();

        // 获取订单归属者 ID
        var context = await _orderService.GetPaymentContextAsync(0, orderId, cancellationToken);
        if (context == null)
            return NotFound(ApiResponse<object>.Fail("ORDER_NOT_FOUND", "订单不存在"));

        await _orderService.CancelAsync(
            context.UserId,  // 订单归属者
            orderId,
            operatorId,      // 管理员自己
            operatorName,    // 管理员姓名
            request?.Reason ?? "后台强制取消",
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, message: "订单已强制取消"));
    }
}

/// <summary>
/// 后台取消订单请求
/// </summary>
public sealed record AdminCancelOrderRequest(string? Reason);
