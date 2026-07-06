using ECommerce.Application.DTOs;

namespace ECommerce.Application.Services;

public interface IAddressService
{
    Task<IReadOnlyList<AddressDto>> GetMyAddressesAsync(long userId, CancellationToken cancellationToken = default);

    Task<long> CreateAsync(long userId, AddressRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(long userId, long addressId, AddressRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(long userId, long addressId, CancellationToken cancellationToken = default);

    Task SetDefaultAsync(long userId, long addressId, CancellationToken cancellationToken = default);
}
