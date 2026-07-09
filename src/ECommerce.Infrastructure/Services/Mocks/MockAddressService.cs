using ECommerce.Application.DTOs;
using ECommerce.Application.Services;

namespace ECommerce.Infrastructure.Services.Mocks;

/// <summary>
/// 临时 Mock 实现，用于解决 IAddressService 依赖注入问题。
/// TEMP_DEMO_ADDRESS: 只用于 member2 地址模块合入前的演示下单。
/// 待 Member2 完成 AddressService 后删除此文件。
/// </summary>
public class MockAddressService : IAddressService
{
    private static readonly IReadOnlyDictionary<long, IReadOnlyList<AddressDto>> DemoAddresses =
        new Dictionary<long, IReadOnlyList<AddressDto>>
        {
            [9003] =
            [
                new AddressDto(
                    AddressId: 9001,
                    ReceiverName: "演示收货人A",
                    ReceiverPhone: "13800009003",
                    Province: "上海市",
                    City: "上海市",
                    District: "浦东新区",
                    DetailAddress: "张江高科演示路 100 号",
                    IsDefault: true,
                    CreatedAt: DateTime.Now.AddDays(-15)
                ),
                new AddressDto(
                    AddressId: 9002,
                    ReceiverName: "演示收货人A",
                    ReceiverPhone: "13800009003",
                    Province: "浙江省",
                    City: "杭州市",
                    District: "西湖区",
                    DetailAddress: "文三路测试小区 8 幢 302",
                    IsDefault: false,
                    CreatedAt: DateTime.Now.AddDays(-10)
                )
            ],
            [9004] =
            [
                new AddressDto(
                    AddressId: 9003,
                    ReceiverName: "演示收货人B",
                    ReceiverPhone: "13800009004",
                    Province: "广东省",
                    City: "深圳市",
                    District: "南山区",
                    DetailAddress: "科技园演示街 66 号",
                    IsDefault: true,
                    CreatedAt: DateTime.Now.AddDays(-8)
                )
            ]
        };

    public Task<IReadOnlyList<AddressDto>> GetMyAddressesAsync(long userId, CancellationToken cancellationToken = default)
    {
        DemoAddresses.TryGetValue(userId, out var addresses);
        return Task.FromResult(addresses ?? Array.Empty<AddressDto>());
    }

    public Task<long> CreateAsync(long userId, AddressRequest request, CancellationToken cancellationToken = default)
    {
        // TEMP_DEMO_ADDRESS: 临时 Mock 只服务演示读取，避免返回数据库中不存在的地址 ID。
        return Task.FromResult(0L);
    }

    public Task UpdateAsync(long userId, long addressId, AddressRequest request, CancellationToken cancellationToken = default)
    {
        // Mock 更新，什么都不做
        return Task.CompletedTask;
    }

    public Task DeleteAsync(long userId, long addressId, CancellationToken cancellationToken = default)
    {
        // Mock 删除，什么都不做
        return Task.CompletedTask;
    }

    public Task SetDefaultAsync(long userId, long addressId, CancellationToken cancellationToken = default)
    {
        // Mock 设置默认地址，什么都不做
        return Task.CompletedTask;
    }
}
