using ECommerce.Shared.Contracts;

namespace ECommerce.Application.DTOs;

public sealed record InventoryAdjustRequest(
    int ChangeQty,
    string Remark);

public sealed record OrderSkuQuantity(
    long SkuId,
    int Quantity);

public sealed record InventoryLogQuery : PageQuery
{
    public long? SkuId { get; init; }

    public string? ChangeType { get; init; }

    public DateTime? StartTime { get; init; }

    public DateTime? EndTime { get; init; }
}

public sealed record InventoryLogDto(
    long LogId,
    long SkuId,
    string ChangeType,
    int ChangeQty,
    int BeforeStock,
    int AfterStock,
    long? OperatorId,
    long? RefOrderId,
    string? Remark,
    DateTime CreatedAt);

public sealed record InventoryWarningDto(
    long SkuId,
    long ProductId,
    string ProductName,
    string SpecDescJson,
    int Stock,
    int LockedStock,
    int WarningStock);
