using ECommerce.Application.DTOs;

namespace ECommerce.Application.Services;

public interface IPaymentService
{
    Task<PaymentDto> CreateOrGetPendingAsync(long userId, long orderId, CancellationToken cancellationToken = default);

    Task<PaymentResultDto> SimulatePayAsync(long userId, SimulatePaymentRequest request, CancellationToken cancellationToken = default);

    Task SyncSimulatedCallbackAsync(SimulatedPaymentCallback request, CancellationToken cancellationToken = default);

    Task<PaymentDto> GetByOrderAsync(long userId, long orderId, CancellationToken cancellationToken = default);
}
