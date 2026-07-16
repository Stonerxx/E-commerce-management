using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Contracts;

namespace ECommerce.Infrastructure.Services.Mocks;

/// <summary>
/// 临时 Mock 实现，用于解决 ICouponService 依赖注入问题。
/// TEMP_DEMO_COUPON: 只用于 member5 优惠券模块合入前的演示下单。
/// 待 Member5 完成 CouponService 后删除此文件。
/// </summary>
public class MockCouponService : ICouponService
{
    public Task<PagedResult<CouponTemplateDto>> SearchTemplatesAsync(CouponTemplateQuery query, CancellationToken cancellationToken = default)
    {
        // 返回空结果
        return Task.FromResult(PagedResult<CouponTemplateDto>.Empty(query.SafePageIndex, query.SafePageSize));
    }

    public Task<int> CreateTemplateAsync(CouponTemplateRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        // Mock 创建，返回一个假 ID
        return Task.FromResult(new Random().Next(1, 100));
    }

    public Task UpdateTemplateAsync(int templateId, CouponTemplateRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        // Mock 更新，什么都不做
        return Task.CompletedTask;
    }

    public Task SetTemplateStatusAsync(int templateId, int status, long operatorId, CancellationToken cancellationToken = default)
    {
        // Mock 更新状态，什么都不做
        return Task.CompletedTask;
    }

    public Task ReceiveAsync(long userId, int templateId, CancellationToken cancellationToken = default)
    {
        // Mock 领取优惠券，什么都不做
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<UserCouponDto>> GetMineAsync(long userId, CancellationToken cancellationToken = default)
    {
        // TEMP_DEMO_COUPON: 返回空列表（没有可用优惠券）。
        return Task.FromResult<IReadOnlyList<UserCouponDto>>(Array.Empty<UserCouponDto>());
    }

    public Task<CouponValidationDto> ValidateAsync(long userId, long userCouponId, decimal orderAmount, CancellationToken cancellationToken = default)
    {
        // TEMP_DEMO_COUPON: 返回一个可用的验证结果，但优惠金额为 0。
        // 这样订单模块可以正常走完流程，只是没有优惠。
        return Task.FromResult(new CouponValidationDto(
            Available: true,
            DiscountAmount: 0m,
            Reason: null
        ));
    }

    public Task UseForOrderAsync(long userId, long userCouponId, long orderId, decimal orderAmount, CancellationToken cancellationToken = default)
    {
        // Mock 核销优惠券，什么都不做
        return Task.CompletedTask;
    }
}
