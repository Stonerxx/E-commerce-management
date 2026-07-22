using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;
using ECommerce.Web.Errors;
using System.Text.Json;

namespace ECommerce.Web.Middleware;

public sealed class BusinessExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly JsonSerializerOptions _jsonOptions;

    public BusinessExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            await HandleBusinessExceptionAsync(context, ex);
        }
    }

    private Task HandleBusinessExceptionAsync(HttpContext context, BusinessException ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = BusinessExceptionStatusMapper.GetStatusCode(ex.Code);

        var response = ApiResponse<object?>.Fail(ex.Code, ex.Message, context.TraceIdentifier);
        var json = JsonSerializer.Serialize(response, _jsonOptions);

        return context.Response.WriteAsync(json);
    }
}
