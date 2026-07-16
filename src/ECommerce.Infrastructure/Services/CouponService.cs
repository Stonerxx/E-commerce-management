using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public class CouponService : ICouponService
{
    private readonly ICouponRepository _couponRepository;

    public CouponService(ICouponRepository couponRepository)
    {
        _couponRepository = couponRepository;
    }

    public async Task<PagedResult<CouponTemplateDto>> SearchTemplatesAsync(CouponTemplateQuery query, CancellationToken cancellationToken = default)
    {
        var result = await _couponRepository.GetTemplatesAsync(query.Keyword, query.Status, query.SafePageIndex, query.SafePageSize, cancellationToken);
        
        var dtos = result.Items.Select(t => new CouponTemplateDto(
            t.Id,
            t.Name,
            t.Type,
            t.FaceValue,
            t.MinConsumption,
            t.TotalIssue,
            t.IssuedCount,
            t.ValidStartTime,
            t.ValidEndTime,
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
            FaceValue = request.Amount,
            MinConsumption = request.MinAmount,
            TotalIssue = request.TotalCount,
            IssuedCount = 0,
            ValidStartTime = request.StartTime,
            ValidEndTime = request.EndTime,
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
        template.FaceValue = request.Amount;
        template.MinConsumption = request.MinAmount;
        template.TotalIssue = request.TotalCount;
        template.ValidStartTime = request.StartTime;
        template.ValidEndTime = request.EndTime;
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

    public Task ReceiveAsync(long userId, int templateId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<UserCouponDto>> GetMineAsync(long userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<UserCouponDto>>(Array.Empty<UserCouponDto>());
    }

    public Task<CouponValidationDto> ValidateAsync(long userId, long userCouponId, decimal orderAmount, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CouponValidationDto(true, 0m, null));
    }

    public Task UseForOrderAsync(long userId, long userCouponId, long orderId, decimal orderAmount, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
