namespace ECommerce.Application.DTOs;

public sealed record CartItemRequest(
    long SkuId,
    int Quantity);

public sealed record UpdateCartItemRequest(
    int Quantity,
    bool Selected);

public sealed record CartItemDto(
    long CartItemId,
    long SkuId,
    long ProductId,
    string ProductName,
    string SpecDescJson,
    string MainImage,
    decimal UnitPrice,
    int Quantity,
    bool Selected,
    DateTime UpdatedAt);

public sealed record CartDto(
    long UserId,
    IReadOnlyList<CartItemDto> Items,
    decimal SelectedTotalAmount);
