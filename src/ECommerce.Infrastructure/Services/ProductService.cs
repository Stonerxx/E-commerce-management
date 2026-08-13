using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IProductImageRepository _productImageRepository;
    private readonly IProductSpecRepository _productSpecRepository;
    private readonly ISkuService _skuService;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(
        IProductRepository productRepository,
        IProductImageRepository productImageRepository,
        IProductSpecRepository productSpecRepository,
        ISkuService skuService,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _productSpecRepository = productSpecRepository;
        _skuService = skuService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<ProductListItemDto>> SearchAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        return await _productRepository.SearchAsync(query, cancellationToken);
    }

    public async Task<PagedResult<ProductListItemDto>> SearchPublicAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        return await _productRepository.SearchPublicAsync(query, cancellationToken);
    }

    public async Task<ProductDetailDto> GetDetailAsync(long productId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            throw new BusinessException("PRODUCT_NOT_FOUND", "商品不存在");
        }

        var images = await _productImageRepository.GetByProductAsync(productId, cancellationToken);
        var specs = await _productSpecRepository.GetByProductAsync(productId, cancellationToken);
        var skus = await _skuService.GetByProductAsync(productId, cancellationToken);

        return MapToDetailDto(product, images, specs, skus);
    }

    public async Task<ProductDetailDto> GetPublicDetailAndTrackAsync(long productId, CancellationToken cancellationToken = default)
    {
        var detail = await GetDetailAsync(productId, cancellationToken);
        if (detail.Status is not ((int)ProductStatus.OnShelf or (int)ProductStatus.Presale))
        {
            throw new BusinessException("PRODUCT_NOT_FOUND", "商品不存在");
        }

        await _productRepository.IncrementViewCountAsync(productId, cancellationToken);
        return detail with { ViewCount = detail.ViewCount + 1 };
    }

    public async Task<IReadOnlyList<ProductListItemDto>> GetRecommendationsAsync(
        long productId,
        int limit = 6,
        CancellationToken cancellationToken = default)
    {
        if (productId <= 0)
        {
            throw new BusinessException("PRODUCT_ID_INVALID", "商品 ID 必须大于 0");
        }

        return await _productRepository.GetRecommendationsAsync(
            productId,
            Math.Clamp(limit, 1, 20),
            cancellationToken);
    }

    public async Task<long> CreateAsync(ProductSaveRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        EnsureSkuRequestCombinationsAreUnique(request.Skus);

        var categoryExists = await _productRepository.CategoryExistsAsync(request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new BusinessException("CATEGORY_NOT_FOUND", "分类不存在");
        }

        var now = DateTime.Now;
        var product = new Product
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Description = request.Description,
            MainImage = request.MainImage,
            Status = request.Status,
            PriceMin = request.Skus.Any() ? request.Skus.Min(s => s.Price) : 0,
            SalesCount = 0,
            ViewCount = 0,
            AvgRating = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        var ownsTransaction = _unitOfWork.CurrentTransaction is null;
        if (ownsTransaction)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            var productId = await _productRepository.CreateAsync(product, cancellationToken);

            foreach (var imageRequest in request.Images)
            {
                var image = new ProductImage
                {
                    ProductId = productId,
                    ImageUrl = imageRequest.ImageUrl,
                    SortOrder = imageRequest.SortOrder,
                    CreatedAt = now
                };
                await _productImageRepository.CreateAsync(image, cancellationToken);
            }

            foreach (var specRequest in request.Specs)
            {
                var spec = new ProductSpec
                {
                    ProductId = productId,
                    SpecName = specRequest.SpecName,
                    SpecValue = specRequest.SpecValue,
                    SortOrder = specRequest.SortOrder,
                    CreatedAt = now
                };
                await _productSpecRepository.CreateAsync(spec, cancellationToken);
            }

            foreach (var skuRequest in request.Skus)
            {
                await _skuService.CreateAsync(productId, skuRequest, operatorId, cancellationToken);
            }

            if (ownsTransaction)
            {
                await _unitOfWork.CommitAsync(cancellationToken);
            }

            return productId;
        }
        catch
        {
            if (ownsTransaction)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    public async Task UpdateAsync(long productId, ProductSaveRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        EnsureSkuRequestCombinationsAreUnique(request.Skus);

        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            throw new BusinessException("PRODUCT_NOT_FOUND", "商品不存在");
        }

        var categoryExists = await _productRepository.CategoryExistsAsync(request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new BusinessException("CATEGORY_NOT_FOUND", "分类不存在");
        }

        var now = DateTime.Now;
        product.CategoryId = request.CategoryId;
        product.Name = request.Name;
        product.Description = request.Description;
        product.MainImage = request.MainImage;
        product.Status = request.Status;
        product.PriceMin = request.Skus.Any() ? request.Skus.Min(s => s.Price) : 0;
        product.UpdatedAt = now;

        var ownsTransaction = _unitOfWork.CurrentTransaction is null;
        if (ownsTransaction)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            await _productRepository.UpdateAsync(product, cancellationToken);

            await _productImageRepository.DeleteByProductAsync(productId, cancellationToken);
            foreach (var imageRequest in request.Images)
            {
                var image = new ProductImage
                {
                    ProductId = productId,
                    ImageUrl = imageRequest.ImageUrl,
                    SortOrder = imageRequest.SortOrder,
                    CreatedAt = now
                };
                await _productImageRepository.CreateAsync(image, cancellationToken);
            }

            await _productSpecRepository.DeleteByProductAsync(productId, cancellationToken);
            foreach (var specRequest in request.Specs)
            {
                var spec = new ProductSpec
                {
                    ProductId = productId,
                    SpecName = specRequest.SpecName,
                    SpecValue = specRequest.SpecValue,
                    SortOrder = specRequest.SortOrder,
                    CreatedAt = now
                };
                await _productSpecRepository.CreateAsync(spec, cancellationToken);
            }

            var existingSkus = await _skuService.GetByProductAsync(productId, cancellationToken);
            var existingSkuIds = existingSkus.Select(sku => sku.SkuId).ToHashSet();
            var retainedSkuIds = new HashSet<long>();
            foreach (var skuRequest in request.Skus)
            {
                if (skuRequest.SkuId is { } skuId)
                {
                    if (!existingSkuIds.Contains(skuId))
                    {
                        throw new BusinessException("SKU_NOT_FOUND", $"SKU {skuId} 不属于当前商品");
                    }
                    if (!retainedSkuIds.Add(skuId))
                    {
                        throw new BusinessException("SKU_DUPLICATE", $"SKU {skuId} 被重复提交");
                    }

                    await _skuService.UpdateAsync(skuId, skuRequest, operatorId, cancellationToken);
                }
                else
                {
                    await _skuService.CreateAsync(productId, skuRequest, operatorId, cancellationToken);
                }
            }

            foreach (var existingSkuId in existingSkuIds.Except(retainedSkuIds))
            {
                await _skuService.RemoveIfUnreferencedOrDisableAsync(existingSkuId, operatorId, cancellationToken);
            }

            if (ownsTransaction)
            {
                await _unitOfWork.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (ownsTransaction)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    public async Task SetStatusAsync(long productId, int status, long operatorId, CancellationToken cancellationToken = default)
    {
        if (status != 0 && status != 1 && status != 2)
        {
            throw new BusinessException("PRODUCT_STATUS_INVALID", "商品状态只能是0（下架）、1（上架）或2（预售）");
        }

        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            throw new BusinessException("PRODUCT_NOT_FOUND", "商品不存在");
        }

        await _productRepository.SetStatusAsync(productId, status, cancellationToken);
    }

    private static void ValidateRequest(ProductSaveRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BusinessException("PRODUCT_NAME_EMPTY", "商品名称不能为空");
        }
        if (request.Name.Length > 200)
        {
            throw new BusinessException("PRODUCT_NAME_TOO_LONG", "商品名称不能超过200个字符");
        }
        if (string.IsNullOrWhiteSpace(request.MainImage))
        {
            throw new BusinessException("PRODUCT_MAIN_IMAGE_EMPTY", "商品主图不能为空");
        }
        if (request.Status != 0 && request.Status != 1 && request.Status != 2)
        {
            throw new BusinessException("PRODUCT_STATUS_INVALID", "商品状态只能是0（下架）、1（上架）或2（预售）");
        }
    }

    private static void EnsureSkuRequestCombinationsAreUnique(IReadOnlyList<SkuSaveRequest> skus)
    {
        var combinations = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sku in skus)
        {
            var combination = string.Join(
                "\u001f",
                sku.SpecSelections
                    .OrderBy(selection => selection.SpecName, StringComparer.Ordinal)
                    .Select(selection => $"{selection.SpecName}\u001e{selection.SpecValue}"));

            if (!combinations.Add(combination))
            {
                throw new BusinessException("SKU_SPEC_COMBINATION_DUPLICATE", "不能提交重复的 SKU 规格组合");
            }
        }
    }

    private static ProductDetailDto MapToDetailDto(
        Product product,
        IReadOnlyList<ProductImageDto> images,
        IReadOnlyList<ProductSpec> specs,
        IReadOnlyList<SkuDto> skus)
    {
        var specDtos = specs.Select(s => new ProductSpecDto(
            SpecId: s.Id,
            SpecName: s.SpecName,
            SpecValue: s.SpecValue,
            SortOrder: s.SortOrder
        )).ToList();

        return new ProductDetailDto(
            ProductId: product.Id,
            CategoryId: product.CategoryId,
            Name: product.Name,
            Description: product.Description,
            MainImage: product.MainImage,
            Status: product.Status,
            PriceMin: product.PriceMin,
            SalesCount: product.SalesCount,
            ViewCount: product.ViewCount,
            AvgRating: product.AvgRating,
            Images: images,
            Specs: specDtos,
            Skus: skus
        );
    }
}
