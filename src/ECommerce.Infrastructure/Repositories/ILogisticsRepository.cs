using ECommerce.Domain.Entities;

namespace ECommerce.Infrastructure.Repositories;

public interface ILogisticsRepository
{
    Task<long> InsertLogisticsAsync(Logistics logistics, CancellationToken cancellationToken = default);
    Task<long> InsertTrackAsync(LogisticsTrack track, CancellationToken cancellationToken = default);
    Task<Logistics?> GetLogisticsByOrderIdAsync(long orderId, CancellationToken cancellationToken = default);
    Task<Logistics?> GetLogisticsByIdAsync(long logisticsId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LogisticsTrack>> GetTracksByLogisticsIdAsync(long logisticsId, CancellationToken cancellationToken = default);
    Task<bool> UpdateLogisticsStatusAsync(long logisticsId, int status, CancellationToken cancellationToken = default);
}
