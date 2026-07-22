using ECommerce.Shared.Contracts;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Exceptions;
using ECommerce.Web.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Web.Filters;

/// <summary>
/// API 统一异常处理，确保错误也符合 ApiResponse JSON 格式。
/// </summary>
public sealed class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;
    private readonly IWebHostEnvironment _environment;

    public ApiExceptionFilter(
        ILogger<ApiExceptionFilter> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public void OnException(ExceptionContext context)
    {
        var traceId = context.HttpContext.TraceIdentifier;

        if (context.Exception is BusinessException businessException)
        {
            context.Result = new ObjectResult(
                ApiResponse<object?>.Fail(businessException.Code, businessException.Message, traceId))
            {
                StatusCode = BusinessExceptionStatusMapper.GetStatusCode(businessException.Code)
            };
            context.ExceptionHandled = true;
            return;
        }

        if (context.Exception is OracleException oracleException)
        {
            _logger.LogError(
                oracleException,
                "Oracle database error {OracleErrorNumber}. TraceId: {TraceId}",
                oracleException.Number,
                traceId);

            var message = _environment.IsDevelopment()
                ? $"Oracle 错误 {oracleException.Number}: {oracleException.Message}"
                : "数据库访问失败，请稍后重试";

            context.Result = new ObjectResult(
                ApiResponse<object?>.Fail("ORACLE_DATABASE_ERROR", message, traceId))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
            context.ExceptionHandled = true;
            return;
        }

        if (context.Exception is UnauthorizedAccessException)
        {
            context.Result = new ObjectResult(
                ApiResponse<object?>.Fail("UNAUTHORIZED", "登录状态无效或已失效", traceId))
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            context.ExceptionHandled = true;
            return;
        }

        if (context.Exception is InvalidOperationException invalidOperationException)
        {
            _logger.LogError(invalidOperationException, "Application configuration or state error. TraceId: {TraceId}", traceId);
            context.Result = new ObjectResult(
                ApiResponse<object?>.Fail(ErrorCodes.ConfigurationError, invalidOperationException.Message, traceId))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
            context.ExceptionHandled = true;
            return;
        }

        _logger.LogError(context.Exception, "Unhandled API error. TraceId: {TraceId}", traceId);
        context.Result = new ObjectResult(
            ApiResponse<object?>.Fail(ErrorCodes.InternalServerError, "服务器内部错误", traceId))
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
        context.ExceptionHandled = true;
    }
}
