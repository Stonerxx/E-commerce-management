// 分页查询、根据ID查询、新增、修改信息、修改启停状态的定义
using ECommerce.Domain.Entities;
using ECommerce.Shared.Contracts;

namespace ECommerce.Infrastructure.Repositories;

public interface ICouponRepository
{
    Task<PagedResult<CouponTemplate>> GetTemplatesAsync(string? keyword, int? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CouponTemplate>> GetAvailableTemplatesAsync(long userId, DateTime now, CancellationToken cancellationToken = default);

    Task<CouponTemplate?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> InsertTemplateAsync(CouponTemplate template, CancellationToken cancellationToken = default);

    Task<bool> UpdateTemplateAsync(CouponTemplate template, CancellationToken cancellationToken = default);

    Task<bool> UpdateTemplateStatusAsync(int id, int status, CancellationToken cancellationToken = default);

    Task<bool> TryIncrementReceivedCountAsync(int templateId, DateTime now, CancellationToken cancellationToken = default);

    Task<long> InsertUserCouponAsync(UserCoupon userCoupon, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserCouponWithTemplate>> GetUserCouponsAsync(long userId, CancellationToken cancellationToken = default);

    Task<UserCouponWithTemplate?> GetUserCouponAsync(long userId, long userCouponId, CancellationToken cancellationToken = default);

    Task<bool> TryUseForOrderAsync(
        long userId,
        long userCouponId,
        long orderId,
        decimal orderAmount,
        decimal expectedDiscountAmount,
        DateTime usedAt,
        CancellationToken cancellationToken = default);

    Task<bool> TryRestoreForOrderAsync(
        long userId,
        long userCouponId,
        long orderId,
        CancellationToken cancellationToken = default);
}

public sealed record UserCouponWithTemplate(UserCoupon UserCoupon, CouponTemplate Template);
