using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Shared.Contracts;

namespace ECommerce.Infrastructure.Repositories;

public interface IOrderRepository
{
    // 写入
    Task<long> InsertOrderMainAsync(OrderMain order, CancellationToken cancellationToken = default);
    Task InsertOrderItemsAsync(IEnumerable<OrderItem> items, CancellationToken cancellationToken = default);
    Task InsertOrderLogAsync(OrderLog log, CancellationToken cancellationToken = default);

    // 更新
    Task<bool> TryUpdateStatusAsync(long orderId, int expectedStatus, int targetStatus, DateTime updatedAt, CancellationToken cancellationToken = default);

    // 查询单条
    Task<OrderMain?> GetOrderByIdAsync(long orderId, CancellationToken cancellationToken = default);
    Task<OrderMain?> GetFullOrderAsync(long orderId, CancellationToken cancellationToken = default);

    // 订单明细/辅助
    Task<IReadOnlyList<OrderSkuQuantity>> GetOrderSkuQuantitiesAsync(long orderId, CancellationToken cancellationToken = default);
    Task<OrderPaymentContextDto?> GetPaymentContextAsync(long orderId, CancellationToken cancellationToken = default);

    // 定时任务
    Task<IReadOnlyList<long>> GetExpiredOrderIdsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default);

    // 分页查询
    Task<PagedResult<OrderListItemDto>> SearchUserOrdersAsync(long userId, OrderQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<OrderListItemDto>> SearchAdminOrdersAsync(AdminOrderQuery query, CancellationToken cancellationToken = default);
}
