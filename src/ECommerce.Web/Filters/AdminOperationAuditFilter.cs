using System.Security.Claims;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ECommerce.Web.Filters;

/// <summary>
/// 补齐后台资源写操作的审计。订单服务已有更细粒度日志，故不在此重复记录订单端点。
/// </summary>
public sealed class AdminOperationAuditFilter : IAsyncActionFilter
{
    private static readonly string[] AuditedPrefixes =
    [
        "/api/v1/admin/categories",
        "/api/v1/admin/products",
        "/api/v1/admin/product-images",
        "/api/v1/admin/skus",
        "/api/v1/admin/users",
        "/api/v1/admin/permissions"
    ];

    private readonly IOperationLogService _operationLogService;
    private readonly ILogger<AdminOperationAuditFilter> _logger;

    public AdminOperationAuditFilter(
        IOperationLogService operationLogService,
        ILogger<AdminOperationAuditFilter> logger)
    {
        _operationLogService = operationLogService;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (!ShouldAudit(request.Method, request.Path))
        {
            await next();
            return;
        }

        var executed = await next();
        if (executed.Exception is not null || executed.Canceled || context.HttpContext.Response.StatusCode >= 400)
        {
            return;
        }

        var userIdText = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId) || userId <= 0)
        {
            return;
        }

        var path = request.Path.Value ?? string.Empty;
        try
        {
            await _operationLogService.WriteAsync(new OperationLogRequest(
                OperatorId: userId,
                OperatorName: context.HttpContext.User.Identity?.Name ?? "未知用户",
                Module: GetModule(path),
                Action: GetActionName(request.Method),
                Description: $"{request.Method} {path}",
                IpAddress: context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                RequestParams: null,
                Result: (int)OperationResult.Success),
                context.HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            // 审计失败不能把已成功的主业务请求变成失败；日志保留给运维排查。
            _logger.LogError(ex, "后台操作审计写入失败: {Method} {Path}", request.Method, path);
        }
    }

    private static bool ShouldAudit(string method, PathString path)
    {
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
        {
            return false;
        }

        return AuditedPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetModule(string path)
    {
        if (path.Contains("/categories", StringComparison.OrdinalIgnoreCase)) return "分类管理";
        if (path.Contains("/product-images", StringComparison.OrdinalIgnoreCase)) return "商品图片管理";
        if (path.Contains("/products", StringComparison.OrdinalIgnoreCase)) return "商品管理";
        if (path.Contains("/skus", StringComparison.OrdinalIgnoreCase)) return "SKU管理";
        if (path.Contains("/users", StringComparison.OrdinalIgnoreCase)) return "用户管理";
        return "权限管理";
    }

    private static string GetActionName(string method) => method.ToUpperInvariant() switch
    {
        "POST" => "创建",
        "PUT" or "PATCH" => "更新",
        "DELETE" => "删除",
        _ => "写操作"
    };
}
