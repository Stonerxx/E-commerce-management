using ECommerce.Domain.Entities;

namespace ECommerce.Infrastructure.Repositories;

public interface ILogisticsRepository
{
    Task<Logistics?> GetByIdAsync(long logisticsId, CancellationToken cancellationToken = default);

    Task<Logistics?> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken = default);

    Task<long> InsertAsync(Logistics logistics, CancellationToken cancellationToken = default);

    Task<long> InsertTrackAsync(LogisticsTrack track, CancellationToken cancellationToken = default);

    Task<bool> TryUpdateStatusAsync(
        long logisticsId,
        int expectedStatus,
        int targetStatus,
        CancellationToken cancellationToken = default);
}
