using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public sealed class OperationLogService : IOperationLogService
{
    private readonly IOperationLogRepository _operationLogRepository;

    public OperationLogService(IOperationLogRepository operationLogRepository)
    {
        _operationLogRepository = operationLogRepository;
    }

    public async Task WriteAsync(OperationLogRequest request, CancellationToken cancellationToken = default)
    {
        if (request.OperatorId <= 0 || string.IsNullOrWhiteSpace(request.OperatorName))
        {
            throw new BusinessException(ErrorCodes.ValidationError, "操作员信息不能为空");
        }

        if (string.IsNullOrWhiteSpace(request.Module) || string.IsNullOrWhiteSpace(request.Action))
        {
            throw new BusinessException(ErrorCodes.ValidationError, "日志模块和动作不能为空");
        }

        await _operationLogRepository.WriteAsync(request, cancellationToken);
    }

    public Task<PagedResult<OperationLogDto>> SearchAsync(OperationLogQuery query, CancellationToken cancellationToken = default)
    {
        return _operationLogRepository.SearchAsync(query, cancellationToken);
    }
}
