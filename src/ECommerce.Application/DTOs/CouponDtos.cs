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
    int Status);

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
    int Status);

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
    DateTime EndTime);

public sealed record CouponValidationRequest(decimal OrderAmount);

public sealed record CouponValidationDto(
    bool Available,
    decimal DiscountAmount,
    string? Reason);
