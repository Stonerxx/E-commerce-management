using ECommerce.Application.DTOs;
using ECommerce.Shared.Contracts;

namespace ECommerce.Application.Services;

public interface ISkuService
{
    Task<SkuDto?> GetByIdAsync(long skuId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkuDto>> GetByProductAsync(long productId, CancellationToken cancellationToken = default);

    Task<PagedResult<AdminSkuListItemDto>> SearchAdminAsync(AdminSkuQuery query, CancellationToken cancellationToken = default);

    Task<long> CreateAsync(long productId, SkuSaveRequest request, long operatorId, CancellationToken cancellationToken = default);

    Task UpdateAsync(long skuId, SkuSaveRequest request, long operatorId, CancellationToken cancellationToken = default);

    Task SetStatusAsync(long skuId, int status, long operatorId, CancellationToken cancellationToken = default);

    Task RemoveIfUnreferencedOrDisableAsync(long skuId, long operatorId, CancellationToken cancellationToken = default);
}
