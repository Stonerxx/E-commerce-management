using System.Security.Claims;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Enums;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Exceptions;
using ECommerce.Web.Controllers;
using ECommerce.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace ECommerce.Tests;

public sealed class PaymentControllerTests
{
    [Fact]
    public async Task Detail_ShowsExpiredNoticeWithoutCreatingPayment()
    {
        var orders = new Mock<IOrderService>();
        var payments = new Mock<IPaymentService>();
        orders.Setup(item => item.GetPaymentContextAsync(7, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(DateTime.Now.AddMinutes(-1)));
        var controller = CreateController(orders.Object, payments.Object);

        var result = await controller.Detail(10);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PaymentViewModel>(view.Model);
        Assert.Null(model.Payment);
        Assert.Contains("支付时间已过期", model.Notice);
        payments.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SimulatePay_RedirectsWithNoticeWhenPaymentExpired()
    {
        var orders = new Mock<IOrderService>();
        var payments = new Mock<IPaymentService>();
        payments.Setup(item => item.SimulatePayAsync(
                7,
                It.Is<SimulatePaymentRequest>(request => request.OrderId == 10),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessException("PAYMENT_EXPIRED", "订单支付时间已过期"));
        var controller = CreateController(orders.Object, payments.Object);

        var result = await controller.SimulatePay(10);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(PaymentController.Detail), redirect.ActionName);
        Assert.Equal(10L, redirect.RouteValues?["orderId"]);
        Assert.Equal("订单支付时间已过期", controller.TempData["PaymentNotice"]);
    }

    private static PaymentController CreateController(IOrderService orders, IPaymentService payments)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "7"),
                new Claim(ClaimTypes.Role, AuthConstants.Roles.User)
            }, AuthConstants.AuthenticationScheme))
        };
        var controller = new PaymentController(orders, payments)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    private static OrderPaymentContextDto CreateOrder(DateTime payExpireTime)
    {
        return new OrderPaymentContextDto(
            10,
            "OD202607230001",
            7,
            (int)OrderStatus.PendingPayment,
            88m,
            null,
            payExpireTime);
    }
}
