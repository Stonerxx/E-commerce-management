using ECommerce.Shared.Contracts;

namespace ECommerce.Application.DTOs;

public sealed record CreateOrderRequest(
    long AddressId,
    long? UserCouponId,
    IReadOnlyList<long> CartItemIds,
    string? Remark);

public record OrderQuery : PageQuery
{
    public int? Status { get; init; }

    public DateTime? StartTime { get; init; }

    public DateTime? EndTime { get; init; }
}

public sealed record AdminOrderQuery : OrderQuery
{
    public long? UserId { get; init; }

    public string? OrderNo { get; init; }
}

public sealed record OrderPreviewDto(
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal PayAmount,
    IReadOnlyList<OrderItemDto> Items);

public sealed record OrderListItemDto(
    long OrderId,
    string OrderNo,
    long UserId,
    int Status,
    decimal PayAmount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record OrderItemDto(
    long OrderItemId,
    long SkuId,
    string ProductName,
    string SpecSnap,
    string MainImage,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal);

public sealed record OrderLogDto(
    long LogId,
    long OrderId,
    int? FromStatus,
    int ToStatus,
    long? OperatorId,
    string? OperatorName,
    string? Remark,
    DateTime CreatedAt);

public sealed record OrderDetailDto(
    long OrderId,
    string OrderNo,
    long UserId,
    long AddressId,
    long? UserCouponId,
    int Status,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal PayAmount,
    DateTime PayExpireTime,
    string ReceiverSnapshotJson,
    string? Remark,
    DateTime CreatedAt,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderLogDto> Logs);

public sealed record OrderPaymentContextDto(
    long OrderId,
    string OrderNo,
    long UserId,
    int Status,
    decimal PayAmount,
    long? UserCouponId,
    DateTime PayExpireTime);
