using ECommerce.Application.Services;
using ECommerce.Domain.Enums;
using ECommerce.Shared.Constants;
using ECommerce.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.Web.Controllers;

[Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
public sealed class PaymentController : Controller
{
    private const long DemoPaymentId = 0;

    private readonly IOrderService _orderService;

    public PaymentController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("/payment/{orderId:long}")]
    public async Task<IActionResult> Detail(long orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var order = await _orderService.GetPaymentContextAsync(userId, orderId, cancellationToken);
        var notice = TempData["PaymentNotice"] as string;
        return View(new DemoPaymentViewModel(order, notice));
    }

    [HttpPost("/payment/{orderId:long}/demo-pay")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemoPay(long orderId, CancellationToken cancellationToken = default)
    {
        // TEMP_DEMO_PAYMENT: 仅用于 member5 支付模块合入前的演示闭环。
        // member5 合入真实 IPaymentService 后，删除此 Action 和对应临时页面。
        var userId = GetCurrentUserId();
        var order = await _orderService.GetPaymentContextAsync(userId, orderId, cancellationToken);

        if (order.Status == (int)OrderStatus.PendingPayment)
        {
            await _orderService.MarkPaidAsync(orderId, DemoPaymentId, cancellationToken);
            TempData["PaymentNotice"] = "TEMP_DEMO_PAYMENT 已模拟支付成功，订单状态已改为已支付。";
        }
        else
        {
            TempData["PaymentNotice"] = "当前订单不是待支付状态，未重复模拟支付。";
        }

        return RedirectToAction(nameof(Detail), new { orderId });
    }

    private long GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(claim) || !long.TryParse(claim, out var userId))
        {
            throw new UnauthorizedAccessException("用户未登录或身份信息无效");
        }

        return userId;
    }
}
