using System.Reflection;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Exceptions;
using ECommerce.Web.Filters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Tests;

public sealed class ApiExceptionFilterTests
{
    [Fact]
    public void BusinessException_PreservesMappedStatusAndContract()
    {
        var context = CreateContext(new BusinessException("PRODUCT_NOT_FOUND", "商品不存在"));

        CreateFilter(Environments.Production).OnException(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        var response = Assert.IsType<ApiResponse<object?>>(result.Value);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        Assert.Equal("PRODUCT_NOT_FOUND", response.Code);
        Assert.Equal("商品不存在", response.Message);
        Assert.True(context.ExceptionHandled);
    }

    [Fact]
    public void UnauthorizedAccessException_Returns401()
    {
        var context = CreateContext(new UnauthorizedAccessException("身份无效"));

        CreateFilter(Environments.Production).OnException(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        var response = Assert.IsType<ApiResponse<object?>>(result.Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        Assert.Equal("UNAUTHORIZED", response.Code);
    }

    [Fact]
    public void InvalidOperationException_UsesSingleConfigurationErrorMapping()
    {
        var context = CreateContext(new InvalidOperationException("配置缺失"));

        CreateFilter(Environments.Production).OnException(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        var response = Assert.IsType<ApiResponse<object?>>(result.Value);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Equal(ErrorCodes.ConfigurationError, response.Code);
        Assert.Equal("配置缺失", response.Message);
    }

    [Fact]
    public void OracleException_InDevelopmentIncludesOracleDetails()
    {
        var exception = CreateOracleException(942, "ORA-00942: table or view does not exist");
        var context = CreateContext(exception);

        CreateFilter(Environments.Development).OnException(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        var response = Assert.IsType<ApiResponse<object?>>(result.Value);
        Assert.Equal("ORACLE_DATABASE_ERROR", response.Code);
        Assert.Contains("Oracle 错误", response.Message);
        Assert.Contains(exception.Number.ToString(), response.Message);
    }

    [Fact]
    public void OracleException_InProductionHidesOracleDetails()
    {
        var exception = CreateOracleException(1017, "ORA-01017: invalid username/password");
        var context = CreateContext(exception);

        CreateFilter(Environments.Production).OnException(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        var response = Assert.IsType<ApiResponse<object?>>(result.Value);
        Assert.Equal("数据库访问失败，请稍后重试", response.Message);
        Assert.DoesNotContain("Oracle", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SQL", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnknownException_ReturnsInternalServerError()
    {
        var context = CreateContext(new Exception("unexpected"));

        CreateFilter(Environments.Production).OnException(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        var response = Assert.IsType<ApiResponse<object?>>(result.Value);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Equal(ErrorCodes.InternalServerError, response.Code);
    }

    private static ApiExceptionFilter CreateFilter(string environmentName)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns(environmentName);
        return new ApiExceptionFilter(Mock.Of<ILogger<ApiExceptionFilter>>(), environment.Object);
    }

    private static OracleException CreateOracleException(int number, string message)
    {
        var constructor = typeof(OracleException).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(int), typeof(string), typeof(string), typeof(string), typeof(int) },
            modifiers: null)
            ?? throw new InvalidOperationException("OracleException test constructor was not found.");

        return (OracleException)constructor.Invoke(new object[] { number, message, "test-db", string.Empty, 0 });
    }

    private static ExceptionContext CreateContext(Exception exception)
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "test-trace"
        };
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());
        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };
    }
}
