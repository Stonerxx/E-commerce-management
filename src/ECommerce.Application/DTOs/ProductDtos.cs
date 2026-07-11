using ECommerce.Shared.Contracts;

namespace ECommerce.Application.DTOs;

public sealed record CategoryRequest(
    int? ParentId,
    string Name,
    int TreeLevel,
    int SortOrder,
    int Status,
    string? IconUrl);

public sealed record CategoryTreeDto(
    int CategoryId,
    int? ParentId,
    string Name,
    int TreeLevel,
    int SortOrder,
    int Status,
    string? IconUrl,
    IReadOnlyList<CategoryTreeDto> Children);

public sealed record ProductImageRequest(
    string ImageUrl,
    int SortOrder);

public sealed record ProductSpecRequest(
    string SpecName,
    string SpecValue,
    int SortOrder);

/// <summary>
/// SKU 规格选择项：用户从商品的 ProductSpec 定义中选择规格名和对应的值
/// </summary>
public sealed record SkuSpecSelection(
    string SpecName,
    string SpecValue);

public sealed record ProductSaveRequest(
    int CategoryId,
    string Name,
    string? Description,
    string MainImage,
    int Status,
    IReadOnlyList<ProductImageRequest> Images,
    IReadOnlyList<ProductSpecRequest> Specs,
    IReadOnlyList<SkuSaveRequest> Skus);

public sealed record SkuSaveRequest(
    IReadOnlyList<SkuSpecSelection> SpecSelections,
    decimal Price,
    decimal? OriginalPrice,
    int Stock,
    int WarningStock,
    string? SkuImage,
    int Status,
    long? SkuId = null);

public sealed record ProductQuery : PageQuery
{
    public int? CategoryId { get; init; }

    public string? Keyword { get; init; }

    public int? Status { get; init; }
}

public sealed record ProductListItemDto(
    long ProductId,
    int CategoryId,
    string Name,
    string MainImage,
    decimal PriceMin,
    int SalesCount,
    decimal AvgRating,
    int Status);

public sealed record ProductImageDto(
    long ImageId,
    string ImageUrl,
    int SortOrder);

public sealed record ProductSpecDto(
    long SpecId,
    string SpecName,
    string SpecValue,
    int SortOrder);

public sealed record SkuDto(
    long SkuId,
    long ProductId,
    string SpecDescJson,
    decimal Price,
    decimal? OriginalPrice,
    int Stock,
    int LockedStock,
    int WarningStock,
    string? SkuImage,
    int Status,
    int ProductStatus = 1);

public sealed record ProductDetailDto(
    long ProductId,
    int CategoryId,
    string Name,
    string? Description,
    string MainImage,
    int Status,
    decimal PriceMin,
    int SalesCount,
    int ViewCount,
    decimal AvgRating,
    IReadOnlyList<ProductImageDto> Images,
    IReadOnlyList<ProductSpecDto> Specs,
    IReadOnlyList<SkuDto> Skus);
