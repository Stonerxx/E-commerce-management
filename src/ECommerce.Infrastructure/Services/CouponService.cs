using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public class CouponService : ICouponService
{
    private readonly ICouponRepository _couponRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CouponService(ICouponRepository couponRepository, IUnitOfWork unitOfWork)
    {
        _couponRepository = couponRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<CouponTemplateDto>> SearchTemplatesAsync(CouponTemplateQuery query, CancellationToken cancellationToken = default)
    {
        var result = await _couponRepository.GetTemplatesAsync(query.Keyword, query.Status, query.PageIndex, query.PageSize, cancellationToken);
        
        var dtos = result.Items.Select(t => new CouponTemplateDto(
            t.Id,
            t.Name,
            t.Type,
            t.Amount,
            t.MinAmount,
            t.TotalCount,
            t.ReceivedCount,
            t.StartTime,
            t.EndTime,
            t.Status
        )).ToList();

        return new PagedResult<CouponTemplateDto>(dtos, result.PageIndex, result.PageSize, result.TotalCount);
    }

    public async Task<int> CreateTemplateAsync(CouponTemplateRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        var template = new CouponTemplate
        {
            Name = request.Name,
            Type = request.Type,
            Amount = request.Amount,
            MinAmount = request.MinAmount,
            TotalCount = request.TotalCount,
            ReceivedCount = 0,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = request.Status
        };

        return await _couponRepository.InsertTemplateAsync(template, cancellationToken);
    }

    public async Task UpdateTemplateAsync(int templateId, CouponTemplateRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        var template = await _couponRepository.GetTemplateByIdAsync(templateId, cancellationToken);
        if (template == null)
        {
            throw new BusinessException("NOT_FOUND", $"Coupon template {templateId} not found");
        }

        template.Name = request.Name;
        template.Type = request.Type;
        template.Amount = request.Amount;
        template.MinAmount = request.MinAmount;
        template.TotalCount = request.TotalCount;
        template.StartTime = request.StartTime;
        template.EndTime = request.EndTime;
        template.Status = request.Status;

        await _couponRepository.UpdateTemplateAsync(template, cancellationToken);
    }

    public async Task SetTemplateStatusAsync(int templateId, int status, long operatorId, CancellationToken cancellationToken = default)
    {
        var success = await _couponRepository.UpdateTemplateStatusAsync(templateId, status, cancellationToken);
        if (!success)
        {
            throw new BusinessException("NOT_FOUND", $"Coupon template {templateId} not found");
        }
    }

    public async Task ReceiveAsync(long userId, int templateId, CancellationToken cancellationToken = default)
    {
        // 开启事务
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var template = await _couponRepository.GetTemplateByIdAsync(templateId, cancellationToken);
            if (template == null)
                throw new BusinessException("NOT_FOUND", "Coupon template not found.");

            if (template.Status != 1)
                throw new BusinessException("INVALID_STATUS", "Coupon template is not active.");

            var now = DateTime.Now;
            if (now < template.StartTime || now > template.EndTime)
                throw new BusinessException("EXPIRED", "Coupon is not within its validity period.");

            // 扣减余量 (增加已领取数)
            bool incremented = await _couponRepository.IncrementTemplateReceivedCountAsync(templateId, cancellationToken);
            if (!incremented)
                throw new BusinessException("OUT_OF_STOCK", "Coupon has reached its issue limit.");

            // 插入领取记录
            var userCoupon = new UserCoupon
            {
                UserId = userId,
                CouponTemplateId = templateId,
                Status = 0 // 0 = 未使用
            };
            await _couponRepository.InsertUserCouponAsync(userCoupon, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<UserCouponDto>> GetMineAsync(long userId, CancellationToken cancellationToken = default)
    {
        var list = await _couponRepository.GetUserCouponsAsync(userId, cancellationToken);
        return list.Select(uc => new UserCouponDto(
            uc.Id,
            uc.UserId,
            uc.CouponTemplateId,
            uc.CouponName,
            uc.Status,
            uc.ReceivedAt,
            uc.UsedAt,
            uc.OrderId
        )).ToList();
    }

    public async Task<CouponValidationDto> ValidateAsync(long userId, long userCouponId, decimal orderAmount, CancellationToken cancellationToken = default)
    {
        var uc = await _couponRepository.GetUserCouponByIdAsync(userCouponId, cancellationToken);
        if (uc == null || uc.UserId != userId)
            return new CouponValidationDto(false, 0m, "Coupon not found or does not belong to you.");

        if (uc.Status != 0)
            return new CouponValidationDto(false, 0m, "Coupon is already used or expired.");

        var template = await _couponRepository.GetTemplateByIdAsync(uc.CouponTemplateId, cancellationToken);
        if (template == null)
            return new CouponValidationDto(false, 0m, "Template not found.");

        var now = DateTime.Now;
        if (now < template.StartTime || now > template.EndTime)
            return new CouponValidationDto(false, 0m, "Coupon is not within its validity period.");

        if (orderAmount < template.MinAmount)
            return new CouponValidationDto(false, 0m, $"Order amount does not meet the minimum requirement of {template.MinAmount}.");

        decimal discountAmount = template.Type == 1 
            ? template.Amount 
            : orderAmount * (1 - template.Amount); // 比如 Amount = 0.85，那么省了 orderAmount * 0.15

        return new CouponValidationDto(true, discountAmount, null);
    }

    public async Task UseForOrderAsync(long userId, long userCouponId, long orderId, decimal orderAmount, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(userId, userCouponId, orderAmount, cancellationToken);
        if (!validation.Available)
        {
            throw new BusinessException("INVALID_COUPON", validation.Reason ?? "Invalid coupon.");
        }

        bool success = await _couponRepository.UpdateUserCouponStatusAsync(userCouponId, 1, orderId, DateTime.Now, cancellationToken);
        if (!success)
        {
            throw new BusinessException("UPDATE_FAILED", "Failed to update coupon status.");
        }
    }
}
