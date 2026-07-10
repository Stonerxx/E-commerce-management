using System.Text.Json;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public sealed class SkuService : ISkuService
{
    private readonly ISkuRepository _skuRepository;
    private readonly IProductSpecRepository _productSpecRepository;

    public SkuService(ISkuRepository skuRepository, IProductSpecRepository productSpecRepository)
    {
        _skuRepository = skuRepository;
        _productSpecRepository = productSpecRepository;
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

        // 校验规格选择必须对应商品已定义的 ProductSpec 记录
        var specDesc = await ValidateAndBuildSpecDescAsync(productId, request.SpecSelections, cancellationToken);

        var now = DateTime.Now;
        var sku = new Sku
        {
            ProductId = productId,
            SpecDesc = specDesc,
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

        if (request.Stock < sku.LockedStock)
        {
            throw new BusinessException(
                "SKU_STOCK_BELOW_LOCKED",
                $"库存不能小于已锁定库存 {sku.LockedStock}");
        }

        // 校验规格选择必须对应商品已定义的 ProductSpec 记录
        var specDesc = await ValidateAndBuildSpecDescAsync(sku.ProductId, request.SpecSelections, cancellationToken);

        var now = DateTime.Now;
        sku.SpecDesc = specDesc;
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

    public async Task RemoveIfUnreferencedOrDisableAsync(long skuId, long operatorId, CancellationToken cancellationToken = default)
    {
        var deleted = await _skuRepository.DeleteIfUnreferencedAsync(skuId, cancellationToken);
        if (deleted == 0)
        {
            await _skuRepository.SetStatusAsync(skuId, 0, cancellationToken);
        }
    }

    /// <summary>
    /// 校验用户提交的规格选择是否全部存在于该商品的 ProductSpec 定义中，
    /// 并将校验通过的选择按 SpecName 排序后序列化为 JSON 存入 SKU.spec_desc。
    /// </summary>
    private async Task<string> ValidateAndBuildSpecDescAsync(
        long productId,
        IReadOnlyList<SkuSpecSelection> selections,
        CancellationToken cancellationToken)
    {
        if (selections == null || selections.Count == 0)
        {
            throw new BusinessException("SKU_SPEC_EMPTY", "SKU必须至少选择一项规格");
        }

        // 检查规格名不重复
        var specNames = selections.Select(s => s.SpecName).ToList();
        if (specNames.Count != specNames.Distinct().Count())
        {
            throw new BusinessException("SKU_SPEC_DUPLICATE", "SKU规格不能有重复的规格名");
        }

        // 获取商品已定义的全部规格选项
        var productSpecs = await _productSpecRepository.GetByProductAsync(productId, cancellationToken);
        if (productSpecs.Count == 0)
        {
            throw new BusinessException("PRODUCT_SPEC_NOT_DEFINED", "该商品尚未定义任何规格，无法创建SKU");
        }

        // 构建 (specName, specValue) 集合用于快速查找
        var definedSpecs = productSpecs
            .Select(s => (s.SpecName, s.SpecValue))
            .ToHashSet();

        // 校验每个选择项都存在于定义中
        foreach (var selection in selections)
        {
            if (string.IsNullOrWhiteSpace(selection.SpecName))
            {
                throw new BusinessException("SKU_SPEC_NAME_EMPTY", "规格名不能为空");
            }
            if (string.IsNullOrWhiteSpace(selection.SpecValue))
            {
                throw new BusinessException("SKU_SPEC_VALUE_EMPTY", "规格值不能为空");
            }
            if (!definedSpecs.Contains((selection.SpecName, selection.SpecValue)))
            {
                throw new BusinessException(
                    "SKU_SPEC_NOT_FOUND",
                    $"规格 \"{selection.SpecName}: {selection.SpecValue}\" 不在该商品的规格定义中");
            }
        }

        // 按 SpecName 排序后构建字典，确保同一组规格生成的 JSON 一致
        var specDict = selections
            .OrderBy(s => s.SpecName)
            .ToDictionary(s => s.SpecName, s => s.SpecValue);

        return JsonSerializer.Serialize(specDict);
    }

    private static void ValidateRequest(SkuSaveRequest request)
    {
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
            Status: sku.Status,
            ProductStatus: sku.ProductStatus
        );
    }
}
