using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/orders")]
[Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
public sealed class OrdersApiController : ApiControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersApiController> _logger;

    public OrdersApiController(IOrderService orderService, ILogger<OrdersApiController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// 订单预览（计算金额、优惠，不生成订单）
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<ApiResponse<OrderPreviewDto>>> Preview(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var preview = await _orderService.PreviewAsync(userId, request, cancellationToken);
        return Ok(ApiResponse<OrderPreviewDto>.Ok(preview));
    }

    /// <summary>
    /// 创建订单
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateOrderResultDto>>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var orderId = await _orderService.CreateAsync(userId, request, cancellationToken);
        return Ok(ApiResponse<CreateOrderResultDto>.Ok(
            new CreateOrderResultDto(orderId),
            message: "订单创建成功，请尽快支付"));
    }

    /// <summary>
    /// 获取当前用户的订单列表（分页）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderListItemDto>>>> SearchMine(
        [FromQuery] OrderQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _orderService.SearchMineAsync(userId, query, cancellationToken);
        return Ok(ApiResponse<PagedResult<OrderListItemDto>>.Ok(result));
    }

    /// <summary>
    /// 获取订单详情
    /// </summary>
    [HttpGet("{orderId:long}")]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> Detail(
        long orderId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var detail = await _orderService.GetDetailAsync(userId, orderId, cancellationToken);
        return Ok(ApiResponse<OrderDetailDto>.Ok(detail));
    }

    /// <summary>
    /// 取消订单
    /// </summary>
    [HttpPost("{orderId:long}/cancel")]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(
        long orderId,
        [FromBody] CancelOrderRequest? request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var userName = GetCurrentUserName();
        var ipAddress = GetClientIpAddress();

        await _orderService.CancelAsync(
            userId,      // 订单归属者
            orderId,
            userId,      // 操作人（用户自己）
            userName,    // 操作人姓名
            ipAddress,
            request?.Reason,
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, message: "订单已取消"));
    }

    /// <summary>
    /// 确认收货
    /// </summary>
    [HttpPost("{orderId:long}/confirm")]
    public async Task<ActionResult<ApiResponse<object>>> Confirm(
        long orderId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        await _orderService.ConfirmAsync(userId, orderId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, message: "已确认收货"));
    }

    /// <summary>
    /// 获取订单日志（已包含在详情中，保留作为独立接口）
    /// </summary>
    [HttpGet("{orderId:long}/logs")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderLogDto>>>> Logs(
        long orderId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var detail = await _orderService.GetDetailAsync(userId, orderId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<OrderLogDto>>.Ok(detail.Logs));
    }
}

/// <summary>
/// 创建订单结果 DTO
/// </summary>
public sealed record CreateOrderResultDto(long OrderId);
