using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IProductImageRepository _productImageRepository;
    private readonly IProductSpecRepository _productSpecRepository;
    private readonly ISkuService _skuService;

    public ProductService(
        IProductRepository productRepository,
        IProductImageRepository productImageRepository,
        IProductSpecRepository productSpecRepository,
        ISkuService skuService)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _productSpecRepository = productSpecRepository;
        _skuService = skuService;
    }

    public async Task<PagedResult<ProductListItemDto>> SearchAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        return await _productRepository.SearchAsync(query, cancellationToken);
    }

    public async Task<ProductDetailDto> GetDetailAsync(long productId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            throw new BusinessException("PRODUCT_NOT_FOUND", "商品不存在");
        }

        await _productRepository.IncrementViewCountAsync(productId, cancellationToken);

        var images = await _productImageRepository.GetByProductAsync(productId, cancellationToken);
        var specs = await _productSpecRepository.GetByProductAsync(productId, cancellationToken);
        var skus = await _skuService.GetByProductAsync(productId, cancellationToken);

        return MapToDetailDto(product, images, specs, skus);
    }

    public async Task<long> CreateAsync(ProductSaveRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

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

        return productId;
    }

    public async Task UpdateAsync(long productId, ProductSaveRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

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

        await _skuService.DeleteByProductAsync(productId, cancellationToken);
        foreach (var skuRequest in request.Skus)
        {
            await _skuService.CreateAsync(productId, skuRequest, operatorId, cancellationToken);
        }
    }

    public async Task SetStatusAsync(long productId, int status, long operatorId, CancellationToken cancellationToken = default)
    {
        if (status != 0 && status != 1)
        {
            throw new BusinessException("PRODUCT_STATUS_INVALID", "商品状态只能是0（下架）或1（上架）");
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
        if (request.Name.Length > 500)
        {
            throw new BusinessException("PRODUCT_NAME_TOO_LONG", "商品名称不能超过500个字符");
        }
        if (string.IsNullOrWhiteSpace(request.MainImage))
        {
            throw new BusinessException("PRODUCT_MAIN_IMAGE_EMPTY", "商品主图不能为空");
        }
        if (request.Status != 0 && request.Status != 1)
        {
            throw new BusinessException("PRODUCT_STATUS_INVALID", "商品状态只能是0（下架）或1（上架）");
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