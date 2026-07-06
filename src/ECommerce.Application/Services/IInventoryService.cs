using ECommerce.Application.DTOs;
using ECommerce.Shared.Contracts;

namespace ECommerce.Application.Services;

public interface IInventoryService
{
    Task AdjustAsync(long skuId, InventoryAdjustRequest request, long operatorId, CancellationToken cancellationToken = default);

    Task LockForOrderAsync(long orderId, IReadOnlyList<OrderSkuQuantity> items, CancellationToken cancellationToken = default);

    Task ReleaseForCancelledOrderAsync(long orderId, IReadOnlyList<OrderSkuQuantity> items, CancellationToken cancellationToken = default);

    Task DeductForPaidOrderAsync(long orderId, IReadOnlyList<OrderSkuQuantity> items, CancellationToken cancellationToken = default);

    Task<PagedResult<InventoryLogDto>> SearchLogsAsync(InventoryLogQuery query, CancellationToken cancellationToken = default);

    Task<PagedResult<InventoryWarningDto>> SearchWarningsAsync(PageQuery query, CancellationToken cancellationToken = default);
}
