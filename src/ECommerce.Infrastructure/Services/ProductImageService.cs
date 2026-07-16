using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public sealed class ProductImageService : IProductImageService
{
    private readonly IProductImageRepository _imageRepository;
    private readonly IProductRepository _productRepository;

    public ProductImageService(IProductImageRepository imageRepository, IProductRepository productRepository)
    {
        _imageRepository = imageRepository;
        _productRepository = productRepository;
    }

    public async Task<long> AddAsync(long productId, ProductImageRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            throw new BusinessException("PRODUCT_IMAGE_URL_EMPTY", "图片URL不能为空");
        }

        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            throw new BusinessException("PRODUCT_NOT_FOUND", "商品不存在");
        }

        var image = new ProductImage
        {
            ProductId = productId,
            ImageUrl = request.ImageUrl,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.Now
        };

        return await _imageRepository.CreateAsync(image, cancellationToken);
    }

    public async Task DeleteAsync(long imageId, long operatorId, CancellationToken cancellationToken = default)
    {
        var rows = await _imageRepository.DeleteAsync(imageId, cancellationToken);
        if (rows == 0)
        {
            throw new BusinessException("PRODUCT_IMAGE_NOT_FOUND", "图片不存在");
        }
    }
}
