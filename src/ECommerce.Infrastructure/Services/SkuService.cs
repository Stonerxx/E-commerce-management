using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public sealed class SkuService : ISkuService
{
    private readonly ISkuRepository _skuRepository;

    public SkuService(ISkuRepository skuRepository)
    {
        _skuRepository = skuRepository;
    }

    public async Task<SkuDto?> GetByIdAsync(long skuId, CancellationToken cancellationToken = default)
    {
        var sku = await _skuRepository.GetByIdAsync(skuId, cancellationToken);
        if (sku == null)
        {
            return null;
        }

        return MapToDto(sku);
    }

    public async Task<IReadOnlyList<SkuDto>> GetByProductAsync(long productId, CancellationToken cancellationToken = default)
    {
        return await _skuRepository.GetByProductAsync(productId, cancellationToken);
    }

    public async Task<long> CreateAsync(long productId, SkuSaveRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var now = DateTime.Now;
        var sku = new Sku
        {
            ProductId = productId,
            SpecDesc = request.SpecDescJson,
            Price = request.Price,
            OriginalPrice = request.OriginalPrice,
            Stock = request.Stock,
            LockedStock = 0,
            WarningStock = request.WarningStock,
            SkuImage = request.SkuImage,
            Status = request.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        return await _skuRepository.CreateAsync(sku, cancellationToken);
    }

    public async Task UpdateAsync(long skuId, SkuSaveRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var sku = await _skuRepository.GetByIdAsync(skuId, cancellationToken);
        if (sku == null)
        {
            throw new BusinessException("SKU_NOT_FOUND", "SKU不存在");
        }

        var now = DateTime.Now;
        sku.SpecDesc = request.SpecDescJson;
        sku.Price = request.Price;
        sku.OriginalPrice = request.OriginalPrice;
        sku.Stock = request.Stock;
        sku.WarningStock = request.WarningStock;
        sku.SkuImage = request.SkuImage;
        sku.Status = request.Status;
        sku.UpdatedAt = now;

        await _skuRepository.UpdateAsync(sku, cancellationToken);
    }

    public async Task SetStatusAsync(long skuId, int status, long operatorId, CancellationToken cancellationToken = default)
    {
        if (status != 0 && status != 1)
        {
            throw new BusinessException("SKU_STATUS_INVALID", "SKU状态只能是0（禁用）或1（启用）");
        }

        var sku = await _skuRepository.GetByIdAsync(skuId, cancellationToken);
        if (sku == null)
        {
            throw new BusinessException("SKU_NOT_FOUND", "SKU不存在");
        }

        await _skuRepository.SetStatusAsync(skuId, status, cancellationToken);
    }

    public async Task DeleteByProductAsync(long productId, CancellationToken cancellationToken = default)
    {
        await _skuRepository.DeleteByProductAsync(productId, cancellationToken);
    }

    private static void ValidateRequest(SkuSaveRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SpecDescJson))
        {
            throw new BusinessException("SKU_SPEC_DESC_EMPTY", "SKU规格描述不能为空");
        }
        if (request.Price < 0)
        {
            throw new BusinessException("SKU_PRICE_INVALID", "SKU价格不能为负数");
        }
        if (request.OriginalPrice.HasValue && request.OriginalPrice.Value < 0)
        {
            throw new BusinessException("SKU_ORIGINAL_PRICE_INVALID", "SKU原价不能为负数");
        }
        if (request.Stock < 0)
        {
            throw new BusinessException("SKU_STOCK_INVALID", "SKU库存不能为负数");
        }
        if (request.WarningStock < 0)
        {
            throw new BusinessException("SKU_WARNING_STOCK_INVALID", "SKU预警库存不能为负数");
        }
        if (request.Status != 0 && request.Status != 1)
        {
            throw new BusinessException("SKU_STATUS_INVALID", "SKU状态只能是0（禁用）或1（启用）");
        }
    }

    private static SkuDto MapToDto(Sku sku)
    {
        return new SkuDto(
            SkuId: sku.Id,
            ProductId: sku.ProductId,
            SpecDescJson: sku.SpecDesc,
            Price: sku.Price,
            OriginalPrice: sku.OriginalPrice,
            Stock: sku.Stock,
            LockedStock: sku.LockedStock,
            WarningStock: sku.WarningStock,
            SkuImage: sku.SkuImage,
            Status: sku.Status
        );
    }
}