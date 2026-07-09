using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services.Mocks;

/// <summary>
/// 临时 Mock 实现，用于解决 IOperationLogService 依赖注入问题。
/// 待 Member2 完成 OperationLogService 后删除此文件。
/// </summary>
public class MockOperationLogService : IOperationLogService
{
    private readonly ILogger<MockOperationLogService> _logger;

    public MockOperationLogService(ILogger<MockOperationLogService> logger)
    {
        _logger = logger;
    }

    public Task WriteAsync(OperationLogRequest request, CancellationToken cancellationToken = default)
    {
        // 记录日志到 ILogger，便于开发调试
        _logger.LogInformation(
            "[MockOperationLog] Module: {Module}, Action: {Action}, Operator: {OperatorName}({OperatorId}), Result: {Result}",
            request.Module,
            request.Action,
            request.OperatorName,
            request.OperatorId,
            request.Result
        );

        // 静默成功，不实际写入数据库
        return Task.CompletedTask;
    }

    public Task<PagedResult<OperationLogDto>> SearchAsync(OperationLogQuery query, CancellationToken cancellationToken = default)
    {
        // 返回空结果
        return Task.FromResult(PagedResult<OperationLogDto>.Empty(query.PageIndex, query.PageSize));
    }
}
