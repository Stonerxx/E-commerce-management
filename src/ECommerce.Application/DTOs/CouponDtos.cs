using ECommerce.Shared.Contracts;

namespace ECommerce.Application.DTOs;

public sealed record CouponTemplateRequest(
    string Name,
    int Type,
    decimal Amount,
    decimal MinAmount,
    int TotalCount,
    DateTime StartTime,
    DateTime EndTime,
    int Status,
    int? ApplicableCategoryId = null);

public sealed record CouponTemplateQuery : PageQuery
{
    public string? Keyword { get; init; }

    public int? Status { get; init; }
}

public sealed record CouponTemplateDto(
    int TemplateId,
    string Name,
    int Type,
    decimal Amount,
    decimal MinAmount,
    int TotalCount,
    int ReceivedCount,
    DateTime StartTime,
    DateTime EndTime,
    int Status,
    int? ApplicableCategoryId = null,
    string? ApplicableCategoryName = null);

public sealed record UserCouponDto(
    long UserCouponId,
    long UserId,
    int CouponTemplateId,
    string CouponName,
    int Status,
    DateTime ReceivedAt,
    DateTime? UsedAt,
    long? OrderId,
    DateTime StartTime,
    DateTime EndTime,
    int Type = 0,
    decimal Amount = 0,
    decimal MinAmount = 0,
    int? ApplicableCategoryId = null,
    string? ApplicableCategoryName = null);

public sealed record CouponValidationRequest(
    decimal OrderAmount = 0,
    IReadOnlyList<long>? CartItemIds = null);

public sealed record CouponValidationDto(
    bool Available,
    decimal DiscountAmount,
    string? Reason,
    decimal EligibleAmount = 0,
    int? ApplicableCategoryId = null,
    string? ApplicableCategoryName = null);
