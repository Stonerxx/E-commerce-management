using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Infrastructure.Services;

public sealed class CouponService : ICouponService
{
    private readonly ICouponRepository _couponRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CouponService(ICouponRepository couponRepository, IUnitOfWork unitOfWork)
    {
        _couponRepository = couponRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<CouponTemplateDto>> SearchTemplatesAsync(
        CouponTemplateQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Status.HasValue && query.Status is not 0 and not 1)
        {
            throw new BusinessException("VALIDATION_ERROR", "优惠券模板状态无效");
        }

        var result = await _couponRepository.GetTemplatesAsync(
            query.Keyword,
            query.Status,
            query.SafePageIndex,
            query.SafePageSize,
            cancellationToken);
        return new PagedResult<CouponTemplateDto>(
            result.Items.Select(MapTemplateDto).ToList(),
            result.PageIndex,
            result.PageSize,
            result.TotalCount);
    }

    public async Task<IReadOnlyList<CouponTemplateDto>> GetAvailableTemplatesAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        var templates = await _couponRepository.GetAvailableTemplatesAsync(userId, DateTime.Now, cancellationToken);
        return templates.Select(MapTemplateDto).ToList();
    }

    public async Task<int> CreateTemplateAsync(
        CouponTemplateRequest request,
        long operatorId,
        CancellationToken cancellationToken = default)
    {
        ValidateTemplateRequest(request);
        return await _couponRepository.InsertTemplateAsync(new CouponTemplate
        {
            Name = request.Name.Trim(),
            Type = request.Type,
            Amount = request.Amount,
            MinAmount = request.MinAmount,
            TotalCount = request.TotalCount,
            ReceivedCount = 0,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = request.Status
        }, cancellationToken);
    }

    public async Task UpdateTemplateAsync(
        int templateId,
        CouponTemplateRequest request,
        long operatorId,
        CancellationToken cancellationToken = default)
    {
        ValidateTemplateRequest(request);
        var template = await _couponRepository.GetTemplateByIdAsync(templateId, cancellationToken)
            ?? throw new BusinessException("COUPON_TEMPLATE_NOT_FOUND", "优惠券模板不存在");
        if (request.TotalCount != -1 && request.TotalCount < template.ReceivedCount)
        {
            throw new BusinessException("VALIDATION_ERROR", "发行总量不能小于已领取数量");
        }

        template.Name = request.Name.Trim();
        template.Type = request.Type;
        template.Amount = request.Amount;
        template.MinAmount = request.MinAmount;
        template.TotalCount = request.TotalCount;
        template.StartTime = request.StartTime;
        template.EndTime = request.EndTime;
        template.Status = request.Status;
        if (!await _couponRepository.UpdateTemplateAsync(template, cancellationToken))
        {
            throw new BusinessException("COUPON_TEMPLATE_NOT_FOUND", "优惠券模板不存在");
        }
    }

    public async Task SetTemplateStatusAsync(
        int templateId,
        int status,
        long operatorId,
        CancellationToken cancellationToken = default)
    {
        if (status is not 0 and not 1)
        {
            throw new BusinessException("VALIDATION_ERROR", "优惠券模板状态无效");
        }

        if (!await _couponRepository.UpdateTemplateStatusAsync(templateId, status, cancellationToken))
        {
            throw new BusinessException("COUPON_TEMPLATE_NOT_FOUND", "优惠券模板不存在");
        }
    }

    public async Task ReceiveAsync(long userId, int templateId, CancellationToken cancellationToken = default)
    {
        var template = await _couponRepository.GetTemplateByIdAsync(templateId, cancellationToken)
            ?? throw new BusinessException("COUPON_TEMPLATE_NOT_FOUND", "优惠券模板不存在");
        var now = DateTime.Now;
        if (!IsTemplateReceivable(template, now))
        {
            throw new BusinessException("COUPON_NOT_AVAILABLE", "优惠券不可领取或已领完");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!await _couponRepository.TryIncrementReceivedCountAsync(templateId, now, cancellationToken))
            {
                throw new BusinessException("COUPON_NOT_AVAILABLE", "优惠券不可领取或已领完");
            }

            await _couponRepository.InsertUserCouponAsync(new UserCoupon
            {
                UserId = userId,
                CouponTemplateId = templateId,
                Status = (int)UserCouponStatus.Unused,
                ReceivedAt = now
            }, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            if (exception is OracleException { Number: 1 })
            {
                throw new BusinessException("COUPON_ALREADY_RECEIVED", "不能重复领取同一优惠券");
            }

            throw;
        }
    }

    public async Task<IReadOnlyList<UserCouponDto>> GetMineAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        var items = await _couponRepository.GetUserCouponsAsync(userId, cancellationToken);
        return items.Select(item => new UserCouponDto(
            item.UserCoupon.Id,
            item.UserCoupon.UserId,
            item.UserCoupon.CouponTemplateId,
            item.Template.Name,
            item.UserCoupon.Status,
            item.UserCoupon.ReceivedAt,
            item.UserCoupon.UsedAt,
            item.UserCoupon.OrderId)).ToList();
    }

    public async Task<CouponValidationDto> ValidateAsync(
        long userId,
        long userCouponId,
        decimal orderAmount,
        CancellationToken cancellationToken = default)
    {
        if (orderAmount <= 0)
        {
            return Unavailable("订单金额必须大于 0");
        }

        var detail = await _couponRepository.GetUserCouponAsync(userId, userCouponId, cancellationToken);
        if (detail is null)
        {
            return Unavailable("优惠券不存在或不属于当前用户");
        }

        if (detail.UserCoupon.Status != (int)UserCouponStatus.Unused)
        {
            return Unavailable("优惠券已使用或已失效");
        }

        var template = detail.Template;
        var now = DateTime.Now;
        if (!IsTemplateActive(template, now))
        {
            return Unavailable("优惠券模板已停用或不在有效期内");
        }

        if (orderAmount < template.MinAmount)
        {
            return Unavailable($"订单金额未达到使用门槛 {template.MinAmount:0.00}");
        }

        decimal discountAmount;
        if (template.Type == (int)CouponType.FullReduction)
        {
            if (template.Amount <= 0 || template.Amount > orderAmount)
            {
                return Unavailable("满减金额无效或超过订单金额");
            }

            discountAmount = template.Amount;
        }
        else if (template.Type == (int)CouponType.Discount)
        {
            if (template.Amount <= 0 || template.Amount > 1)
            {
                return Unavailable("折扣率无效");
            }

            discountAmount = Math.Round(orderAmount * (1 - template.Amount), 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            return Unavailable("优惠券类型无效");
        }

        return new CouponValidationDto(true, discountAmount, null);
    }

    public async Task UseForOrderAsync(
        long userId,
        long userCouponId,
        long orderId,
        decimal orderAmount,
        decimal expectedDiscountAmount,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(userId, userCouponId, orderAmount, cancellationToken);
        if (!validation.Available)
        {
            throw new BusinessException("COUPON_NOT_AVAILABLE", validation.Reason ?? "优惠券不可用");
        }

        if (validation.DiscountAmount != expectedDiscountAmount)
        {
            throw new BusinessException("COUPON_CHANGED", "优惠券规则已变化，请刷新订单后重试");
        }

        if (!await _couponRepository.TryUseForOrderAsync(
                userId,
                userCouponId,
                orderId,
                orderAmount,
                expectedDiscountAmount,
                DateTime.Now,
                cancellationToken))
        {
            throw new BusinessException("COUPON_ALREADY_USED", "优惠券状态已变化，请刷新后重试");
        }
    }

    public async Task RestoreForOrderAsync(
        long userId,
        long userCouponId,
        long orderId,
        CancellationToken cancellationToken = default)
    {
        if (!await _couponRepository.TryRestoreForOrderAsync(userId, userCouponId, orderId, cancellationToken))
        {
            throw new BusinessException("COUPON_RESTORE_FAILED", "订单优惠券状态不一致，取消失败");
        }
    }

    private static void ValidateTemplateRequest(CouponTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
        {
            throw new BusinessException("VALIDATION_ERROR", "优惠券名称不能为空且不能超过 100 个字符");
        }

        if (request.Status is not 0 and not 1)
        {
            throw new BusinessException("VALIDATION_ERROR", "优惠券模板状态无效");
        }

        if (request.TotalCount < -1)
        {
            throw new BusinessException("VALIDATION_ERROR", "发行总量只能为 -1 或非负数");
        }

        if (request.MinAmount < 0 || request.EndTime <= request.StartTime)
        {
            throw new BusinessException("VALIDATION_ERROR", "最低消费不能为负数且结束时间必须晚于开始时间");
        }

        if (request.Type == (int)CouponType.FullReduction)
        {
            if (request.Amount <= 0 || request.MinAmount < request.Amount)
            {
                throw new BusinessException("VALIDATION_ERROR", "满减金额必须大于 0 且不能超过使用门槛");
            }
        }
        else if (request.Type == (int)CouponType.Discount)
        {
            if (request.Amount <= 0 || request.Amount > 1)
            {
                throw new BusinessException("VALIDATION_ERROR", "折扣率必须大于 0 且不超过 1");
            }
        }
        else
        {
            throw new BusinessException("VALIDATION_ERROR", "优惠券类型无效");
        }
    }

    private static bool IsTemplateActive(CouponTemplate template, DateTime now)
    {
        return template.Status == 1
            && template.StartTime <= now
            && template.EndTime >= now;
    }

    private static bool IsTemplateReceivable(CouponTemplate template, DateTime now)
    {
        return IsTemplateActive(template, now)
            && (template.TotalCount == -1 || template.ReceivedCount < template.TotalCount);
    }

    private static CouponTemplateDto MapTemplateDto(CouponTemplate template)
    {
        return new CouponTemplateDto(
            template.Id,
            template.Name,
            template.Type,
            template.Amount,
            template.MinAmount,
            template.TotalCount,
            template.ReceivedCount,
            template.StartTime,
            template.EndTime,
            template.Status);
    }

    private static CouponValidationDto Unavailable(string reason)
    {
        return new CouponValidationDto(false, 0, reason);
    }
}
