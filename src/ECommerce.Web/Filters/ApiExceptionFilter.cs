using ECommerce.Shared.Contracts;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Web.Filters;

/// <summary>
/// API 统一异常处理，保证错误响应也符合 ApiResponse JSON 格式。
/// </summary>
public sealed class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;
    private readonly IWebHostEnvironment _environment;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public void OnException(ExceptionContext context)
    {
        var traceId = context.HttpContext.TraceIdentifier;

        if (context.Exception is BusinessException businessException)
        {
            context.Result = new BadRequestObjectResult(
                ApiResponse<object?>.Fail(businessException.Code, businessException.Message, traceId));
            context.ExceptionHandled = true;
            return;
        }

        if (context.Exception is OracleException oracleException)
        {
            _logger.LogError(oracleException, "Oracle database error. Number: {Number}, TraceId: {TraceId}", oracleException.Number, traceId);

            var message = _environment.IsDevelopment()
                ? $"数据库访问失败：ORA-{oracleException.Number}，{oracleException.Message}"
                : "数据库访问失败，请检查网络、环境变量或SQL语句";

            context.Result = new ObjectResult(
                ApiResponse<object?>.Fail("ORACLE_DATABASE_ERROR", message, traceId))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
            context.ExceptionHandled = true;
            return;
        }

        if (context.Exception is InvalidOperationException invalidOperationException)
        {
            _logger.LogError(invalidOperationException, "Configuration or runtime error. TraceId: {TraceId}", traceId);
            context.Result = new ObjectResult(
                ApiResponse<object?>.Fail(ErrorCodes.ValidationError, invalidOperationException.Message, traceId))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
            context.ExceptionHandled = true;
            return;
        }

        _logger.LogError(context.Exception, "Unhandled API error. TraceId: {TraceId}", traceId);
        context.Result = new ObjectResult(
            ApiResponse<object?>.Fail(ErrorCodes.ValidationError, "服务器内部错误", traceId))
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
        context.ExceptionHandled = true;
    }
}
