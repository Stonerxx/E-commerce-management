using ECommerce.Application.DTOs;
using ECommerce.Shared.Contracts;

namespace ECommerce.Application.Services;

public interface IOrderService
{
    Task<OrderPreviewDto> PreviewAsync(long userId, CreateOrderRequest request, CancellationToken cancellationToken = default);

    Task<CouponValidationDto> ValidateCouponAsync(
        long userId,
        long userCouponId,
        IReadOnlyList<long>? cartItemIds,
        CancellationToken cancellationToken = default);

    Task<long> CreateAsync(long userId, CreateOrderRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<OrderListItemDto>> SearchMineAsync(long userId, OrderQuery query, CancellationToken cancellationToken = default);

    Task<PagedResult<OrderListItemDto>> SearchAdminAsync(AdminOrderQuery query, CancellationToken cancellationToken = default);

    Task<OrderDetailDto> GetDetailAsync(long userId, long orderId, CancellationToken cancellationToken = default);

    Task<OrderDetailDto> GetAdminDetailAsync(long orderId, CancellationToken cancellationToken = default);

    Task<OrderPaymentContextDto> GetPaymentContextAsync(long userId, long orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderSkuQuantity>> GetSkuQuantitiesAsync(long orderId, CancellationToken cancellationToken = default);

    Task CancelAsync(long userId, long orderId, long operatorId, string operatorName, string ipAddress, string? reason, CancellationToken cancellationToken = default);

    Task ConfirmAsync(long userId, long orderId, CancellationToken cancellationToken = default);

    Task MarkPaidAsync(long orderId, long paymentId, CancellationToken cancellationToken = default);

    Task MarkShippedAsync(long orderId, long logisticsId, long operatorId, string operatorName, string ipAddress, CancellationToken cancellationToken = default);
}
