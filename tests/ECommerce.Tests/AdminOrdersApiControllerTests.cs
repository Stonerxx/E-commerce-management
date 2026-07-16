using System.Security.Claims;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Web.Controllers.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Tests;

public sealed class AdminOrdersApiControllerTests
{
    [Fact]
    public async Task AdminCancel_ShouldUseAdminDetailInsteadOfCustomerPaymentContext()
    {
        var orderService = new Mock<IOrderService>();
        var logger = new Mock<ILogger<AdminOrdersApiController>>();
        var orderId = 9001L;
        var ownerUserId = 9003L;
        var adminUserId = 9001L;

        orderService
            .Setup(x => x.GetAdminDetailAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderDetailDto(
                orderId,
                "DEMO202607080001",
                ownerUserId,
                9001,
                null,
                0,
                3299m,
                0m,
                3299m,
                DateTime.UtcNow.AddMinutes(30),
                "{}",
                null,
                DateTime.UtcNow,
                Array.Empty<OrderItemDto>(),
                Array.Empty<OrderLogDto>()));

        orderService
            .Setup(x => x.CancelAsync(
                ownerUserId,
                orderId,
                adminUserId,
                "demo_admin",
                It.IsAny<string>(),
                "后台强制取消",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new AdminOrdersApiController(orderService.Object, logger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, adminUserId.ToString()),
                        new Claim(ClaimTypes.Name, "demo_admin"),
                        new Claim(ClaimTypes.Role, AuthConstants.Roles.Admin)
                    }, AuthConstants.AuthenticationScheme))
                }
            }
        };

        var result = await controller.AdminCancel(orderId, new AdminCancelOrderRequest("后台强制取消"));

        Assert.IsType<OkObjectResult>(result.Result);
        orderService.Verify(x => x.GetAdminDetailAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
        orderService.Verify(x => x.GetPaymentContextAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        orderService.VerifyAll();
    }
}
