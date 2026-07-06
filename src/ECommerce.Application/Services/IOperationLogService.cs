using ECommerce.Application.DTOs;
using ECommerce.Shared.Contracts;

namespace ECommerce.Application.Services;

public interface IOperationLogService
{
    Task WriteAsync(OperationLogRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<OperationLogDto>> SearchAsync(OperationLogQuery query, CancellationToken cancellationToken = default);
}
