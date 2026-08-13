using ECommerce.Shared.Exceptions;
using ECommerce.Web.Controllers;
using ECommerce.Web.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests;

public sealed class HomeControllerTests
{
    [Fact]
    public void Error_ShowsBusinessMessageAndTraceId()
    {
        var controller = CreateController(new BusinessException("ORDER_NOT_FOUND", "订单不存在"));

        var result = Assert.IsType<ViewResult>(controller.Error());
        var model = Assert.IsType<ErrorViewModel>(result.Model);

        Assert.Equal("订单不存在", model.Message);
        Assert.Equal("test-trace", model.RequestId);
    }

    [Fact]
    public void Error_HidesUnexpectedExceptionDetails()
    {
        var controller = CreateController(new InvalidOperationException("sensitive detail"));

        var result = Assert.IsType<ViewResult>(controller.Error());
        var model = Assert.IsType<ErrorViewModel>(result.Model);

        Assert.Equal("操作未能完成，请稍后重试。", model.Message);
        Assert.DoesNotContain("sensitive", model.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static HomeController CreateController(Exception exception)
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "test-trace"
        };
        httpContext.Features.Set<IExceptionHandlerPathFeature>(new ExceptionHandlerFeature
        {
            Error = exception,
            Path = "/payment/10/demo-pay"
        });

        return new HomeController(Mock.Of<ILogger<HomeController>>())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}
