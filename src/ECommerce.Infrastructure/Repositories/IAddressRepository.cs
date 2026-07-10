using ECommerce.Application.DTOs;

namespace ECommerce.Infrastructure.Repositories;

public interface IAddressRepository
{
    Task<IReadOnlyList<AddressDto>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    Task<AddressDto?> GetByIdAsync(long userId, long addressId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(long userId, long addressId, CancellationToken cancellationToken = default);

    Task<long> CreateAsync(long userId, AddressRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(long userId, long addressId, AddressRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(long userId, long addressId, CancellationToken cancellationToken = default);

    Task SetDefaultAsync(long userId, long addressId, CancellationToken cancellationToken = default);
}
