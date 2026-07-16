using ECommerce.Application.DTOs;
using ECommerce.Application.Services;

namespace ECommerce.Infrastructure.Services.Mocks;

/// <summary>
/// 临时 Mock 实现，用于解决 ISkuService 依赖注入问题。
/// TEMP_DEMO_SKU: 只用于 member3 SKU 模块合入前的演示下单。
/// 待 Member3 完成 SkuService 后删除此文件。
/// </summary>
public class MockSkuService : ISkuService
{
    public Task<SkuDto?> GetByIdAsync(long skuId, CancellationToken cancellationToken = default)
    {
        // TEMP_DEMO_SKU: 返回一个模拟的 SKU 数据，满足基本校验需要。
        var mockSku = new SkuDto(
            SkuId: skuId,
            ProductId: 1,
            SpecDescJson: "{\"颜色\":\"红色\",\"尺码\":\"M\"}",
            Price: 99.99m,
            OriginalPrice: 129.99m,
            Stock: 100,
            LockedStock: 0,
            WarningStock: 10,
            SkuImage: "/images/sku-default.jpg",
            Status: 1  // 在售
        );
        return Task.FromResult<SkuDto?>(mockSku);
    }

    public Task<IReadOnlyList<SkuDto>> GetByProductAsync(long productId, CancellationToken cancellationToken = default)
    {
        // 返回一个模拟列表
        var mockList = new List<SkuDto>
        {
            new(1, productId, "{\"颜色\":\"红色\",\"尺码\":\"M\"}", 99.99m, 129.99m, 100, 0, 10, "/images/sku1.jpg", 1),
            new(2, productId, "{\"颜色\":\"红色\",\"尺码\":\"L\"}", 99.99m, 129.99m, 80, 0, 10, "/images/sku2.jpg", 1),
        };
        return Task.FromResult<IReadOnlyList<SkuDto>>(mockList);
    }

    public Task<long> CreateAsync(long productId, SkuSaveRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        // Mock 创建，返回一个假 ID
        return Task.FromResult((long)(new Random().Next(1000, 9999)));
    }

    public Task UpdateAsync(long skuId, SkuSaveRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        // Mock 更新，什么都不做
        return Task.CompletedTask;
    }

    public Task SetStatusAsync(long skuId, int status, long operatorId, CancellationToken cancellationToken = default)
    {
        // Mock 更新状态
        return Task.CompletedTask;
    }

    public Task RemoveIfUnreferencedOrDisableAsync(long skuId, long operatorId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
