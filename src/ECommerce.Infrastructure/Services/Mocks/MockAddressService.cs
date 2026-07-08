using ECommerce.Application.DTOs;
using ECommerce.Application.Services;

namespace ECommerce.Infrastructure.Services.Mocks;

/// <summary>
/// 临时 Mock 实现，用于解决 IAddressService 依赖注入问题。
/// 待 Member2 完成 AddressService 后删除此文件。
/// </summary>
public class MockAddressService : IAddressService
{
    private readonly List<AddressDto> _mockAddresses = new()
    {
        new AddressDto(
            AddressId: 1,
            ReceiverName: "张三",
            ReceiverPhone: "13800001111",
            Province: "广东省",
            City: "深圳市",
            District: "南山区",
            DetailAddress: "科技园南区XX大厦A座1001",
            IsDefault: true,
            CreatedAt: DateTime.Now.AddDays(-30)
        ),
        new AddressDto(
            AddressId: 2,
            ReceiverName: "李四",
            ReceiverPhone: "13800002222",
            Province: "广东省",
            City: "广州市",
            District: "天河区",
            DetailAddress: "天河路XX号B座205",
            IsDefault: false,
            CreatedAt: DateTime.Now.AddDays(-15)
        )
    };

    public Task<IReadOnlyList<AddressDto>> GetMyAddressesAsync(long userId, CancellationToken cancellationToken = default)
    {
        // 返回模拟地址列表
        return Task.FromResult<IReadOnlyList<AddressDto>>(_mockAddresses);
    }

    public Task<long> CreateAsync(long userId, AddressRequest request, CancellationToken cancellationToken = default)
    {
        // 模拟创建，返回一个新的 ID
        var newId = _mockAddresses.Max(x => x.AddressId) + 1;
        return Task.FromResult(newId);
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
