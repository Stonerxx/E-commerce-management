using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Enums;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Exceptions;
using ECommerce.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.Web.Controllers;

[Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
public sealed class PaymentController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;

    public PaymentController(IOrderService orderService, IPaymentService paymentService)
    {
        _orderService = orderService;
        _paymentService = paymentService;
    }

    [HttpGet("/payment/{orderId:long}")]
    public async Task<IActionResult> Detail(long orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var order = await _orderService.GetPaymentContextAsync(userId, orderId, cancellationToken);
        var notice = TempData["PaymentNotice"] as string;
        PaymentDto? payment = null;

        if (order.Status == (int)OrderStatus.PendingPayment && order.PayExpireTime <= DateTime.Now)
        {
            notice ??= "订单支付时间已过期，此订单无法继续支付。";
        }
        else
        {
            try
            {
                payment = order.Status == (int)OrderStatus.PendingPayment
                    ? await _paymentService.CreateOrGetPendingAsync(userId, orderId, cancellationToken)
                    : await _paymentService.GetByOrderAsync(userId, orderId, cancellationToken);
            }
            catch (BusinessException ex)
            {
                notice ??= ex.Message;
            }
        }

        return View(new PaymentViewModel(order, payment, notice));
    }

    [HttpPost("/payment/{orderId:long}/demo-pay")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SimulatePay(long orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        try
        {
            var result = await _paymentService.SimulatePayAsync(
                userId,
                new SimulatePaymentRequest(orderId, "模拟支付"),
                cancellationToken);
            TempData["PaymentNotice"] = result.Paid
                ? $"支付成功，交易流水号：{result.TradeNo}"
                : "支付尚未完成。";
        }
        catch (BusinessException ex)
        {
            TempData["PaymentNotice"] = ex.Message;
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
