using ECommerce.Domain.Entities;

namespace ECommerce.Infrastructure.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken = default);

    Task<long> InsertAsync(Payment payment, CancellationToken cancellationToken = default);

    Task<bool> TryMarkSuccessAsync(
        long paymentId,
        decimal expectedAmount,
        string tradeNo,
        DateTime paidAt,
        string callbackData,
        CancellationToken cancellationToken = default);

    Task<bool> TryMarkFailedAsync(
        long paymentId,
        string callbackData,
        CancellationToken cancellationToken = default);
}
