using ECommerce.Application.DTOs;

namespace ECommerce.Application.Services;

public interface ILogisticsService
{
    Task ShipAsync(
        long orderId,
        ShipmentRequest request,
        long operatorId,
        string operatorName,
        string ipAddress,
        CancellationToken cancellationToken = default);

    Task AddTrackAsync(
        long logisticsId,
        LogisticsTrackRequest request,
        long operatorId,
        string operatorName,
        string ipAddress,
        CancellationToken cancellationToken = default);

    Task<LogisticsDto?> GetByOrderAsync(long userId, long orderId, CancellationToken cancellationToken = default);

    Task<LogisticsDto?> GetByOrderAdminAsync(long orderId, CancellationToken cancellationToken = default);
}
