using ECommerce.Application.DTOs;

namespace ECommerce.Application.Services;

public interface ILogisticsService
{
    Task ShipAsync(long orderId, ShipmentRequest request, long operatorId, CancellationToken cancellationToken = default);

    Task AddTrackAsync(long logisticsId, LogisticsTrackRequest request, long operatorId, CancellationToken cancellationToken = default);

    Task<LogisticsDto?> GetByOrderAsync(long userId, long orderId, CancellationToken cancellationToken = default);
}
