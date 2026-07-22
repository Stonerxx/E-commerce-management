using ECommerce.Application.DTOs;
using ECommerce.Shared.Contracts;

namespace ECommerce.Infrastructure.Repositories;

public interface IOperationLogRepository
{
    Task WriteAsync(OperationLogRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<OperationLogDto>> SearchAsync(OperationLogQuery query, CancellationToken cancellationToken = default);
}
