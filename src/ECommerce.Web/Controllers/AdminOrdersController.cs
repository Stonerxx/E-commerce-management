using ECommerce.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

/// <summary>
/// 后台订单管理页面
/// </summary>
[Authorize(Policy = AuthConstants.Policies.ServiceOrAdmin)]
public sealed class AdminOrdersController : Controller
{
    private readonly ILogger<AdminOrdersController> _logger;

    public AdminOrdersController(ILogger<AdminOrdersController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 后台订单列表页
    /// </summary>
    [HttpGet("/admin/orders")]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// 后台订单详情页
    /// </summary>
    [HttpGet("/admin/orders/{orderId:long}")]
    public IActionResult Detail(long orderId)
    {
        ViewData["OrderId"] = orderId;
        return View();
    }
}
