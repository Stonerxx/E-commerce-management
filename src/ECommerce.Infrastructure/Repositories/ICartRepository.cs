using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Infrastructure.Repositories;

public interface ICartRepository
{
    /// <summary> 获取用户购物车明细（联表 SKU + Product） </summary>
    Task<IReadOnlyList<CartItemWithDetails>> GetUserCartWithDetailsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary> 根据用户和 SKU 查询单条购物车记录 </summary>
    Task<Cart?> GetByUserAndSkuAsync(long userId, long skuId, CancellationToken cancellationToken = default);

    /// <summary> 原子增加已有购物车项的数量，并保证不超过可用库存。 </summary>
    Task<int> TryIncreaseQuantityAsync(long userId, long skuId, int quantity, int maximumQuantity, DateTime updatedAt, CancellationToken cancellationToken = default);

    /// <summary> 批量获取指定的购物车项（用于下单） </summary>
    Task<IReadOnlyList<Cart>> GetByIdsAsync(IReadOnlyList<long> cartItemIds, CancellationToken cancellationToken = default);

    /// <summary> 新增购物车记录 </summary>
    Task AddAsync(Cart cart, CancellationToken cancellationToken = default);

    /// <summary> 更新购物车记录 </summary>
    Task UpdateAsync(Cart cart, CancellationToken cancellationToken = default);

    /// <summary> 删除单条购物车记录 </summary>
    Task RemoveAsync(long cartItemId, CancellationToken cancellationToken = default);

    /// <summary> 清空用户所有选中的购物车项（selected=1） </summary>
    Task ClearSelectedAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary> 清空用户本次下单指定的购物车项 </summary>
    Task ClearByIdsAsync(long userId, IReadOnlyList<long> cartItemIds, CancellationToken cancellationToken = default);

    /// <summary> 清空用户所有购物车项 </summary>
    Task ClearAllAsync(long userId, CancellationToken cancellationToken = default);
}
