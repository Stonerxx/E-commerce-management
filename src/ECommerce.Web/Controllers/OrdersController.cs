using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

/// <summary>
/// 用户订单页面
/// </summary>
[Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
public sealed class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// 我的订单列表页
    /// </summary>
    [HttpGet("/orders")]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// 订单详情页
    /// </summary>
    [HttpGet("/orders/{orderId:long}")]
    public async Task<IActionResult> Detail(long orderId, CancellationToken cancellationToken = default)
    {
        ViewData["OrderId"] = orderId;
        return View();
    }

    /// <summary>
    /// 订单确认/结算页
    /// </summary>
    [HttpGet("/orders/create")]
    public IActionResult Create([FromQuery] string cartItemIds)
    {
        // 将选中的购物车项 ID 传给前端，用于预览和提交
        ViewData["CartItemIds"] = cartItemIds;
        return View();
    }
}
