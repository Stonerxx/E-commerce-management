using ECommerce.Application.DTOs;
using ECommerce.Shared.Contracts;

namespace ECommerce.Application.Services;

public interface ICouponService
{
    Task<PagedResult<CouponTemplateDto>> SearchTemplatesAsync(CouponTemplateQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CouponTemplateDto>> GetAvailableTemplatesAsync(long userId, CancellationToken cancellationToken = default);

    Task<int> CreateTemplateAsync(CouponTemplateRequest request, long operatorId, CancellationToken cancellationToken = default);

    Task UpdateTemplateAsync(int templateId, CouponTemplateRequest request, long operatorId, CancellationToken cancellationToken = default);

    Task SetTemplateStatusAsync(int templateId, int status, long operatorId, CancellationToken cancellationToken = default);

    Task ReceiveAsync(long userId, int templateId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserCouponDto>> GetMineAsync(long userId, CancellationToken cancellationToken = default);

    Task<CouponValidationDto> ValidateAsync(long userId, long userCouponId, decimal orderAmount, CancellationToken cancellationToken = default);

    Task<CouponValidationDto> ValidateAsync(
        long userId,
        long userCouponId,
        decimal orderAmount,
        IReadOnlyDictionary<int, decimal> categoryAmounts,
        CancellationToken cancellationToken = default);

    Task UseForOrderAsync(
        long userId,
        long userCouponId,
        long orderId,
        decimal orderAmount,
        decimal expectedDiscountAmount,
        CancellationToken cancellationToken = default);

    Task UseForOrderAsync(
        long userId,
        long userCouponId,
        long orderId,
        decimal orderAmount,
        IReadOnlyDictionary<int, decimal> categoryAmounts,
        decimal expectedDiscountAmount,
        CancellationToken cancellationToken = default);

    Task RestoreForOrderAsync(long userId, long userCouponId, long orderId, CancellationToken cancellationToken = default);
}
