using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Contracts;

namespace ECommerce.Infrastructure.Services.Mocks;

/// <summary>
/// 临时 Mock 实现，用于解决 IInventoryService 依赖注入问题。
/// 待 Member3 完成 InventoryService 后删除此文件。
/// </summary>
public class MockInventoryService : IInventoryService
{
    public Task AdjustAsync(long skuId, InventoryAdjustRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        // Mock 调整库存，什么都不做
        return Task.CompletedTask;
    }

    public Task LockForOrderAsync(long orderId, IReadOnlyList<OrderSkuQuantity> items, CancellationToken cancellationToken = default)
    {
        // Mock 锁定库存，静默成功（订单模块可继续执行）
        return Task.CompletedTask;
    }

    public Task ReleaseForCancelledOrderAsync(long orderId, IReadOnlyList<OrderSkuQuantity> items, CancellationToken cancellationToken = default)
    {
        // Mock 释放锁定库存，静默成功
        return Task.CompletedTask;
    }

    public Task DeductForPaidOrderAsync(long orderId, IReadOnlyList<OrderSkuQuantity> items, CancellationToken cancellationToken = default)
    {
        // Mock 扣减库存，静默成功
        return Task.CompletedTask;
    }

    public Task<PagedResult<InventoryLogDto>> SearchLogsAsync(InventoryLogQuery query, CancellationToken cancellationToken = default)
    {
        // 返回空结果
        return Task.FromResult(PagedResult<InventoryLogDto>.Empty(query.PageIndex, query.PageSize));
    }

    public Task<PagedResult<InventoryWarningDto>> SearchWarningsAsync(PageQuery query, CancellationToken cancellationToken = default)
    {
        // 返回空结果
        return Task.FromResult(PagedResult<InventoryWarningDto>.Empty(query.PageIndex, query.PageSize));
    }
}
