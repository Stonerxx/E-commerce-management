using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Models;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Infrastructure.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly ISkuService _skuService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CartService> _logger;

    public CartService(
        ICartRepository cartRepository,
        ISkuService skuService,
        IUnitOfWork unitOfWork,
        ILogger<CartService> logger)
    {
        _cartRepository = cartRepository;
        _skuService = skuService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CartDto> GetCartAsync(long userId, CancellationToken cancellationToken = default)
    {
        var items = await _cartRepository.GetUserCartWithDetailsAsync(userId, cancellationToken);
        var dtos = items.Select(item => new CartItemDto(
            item.CartItemId,
            item.SkuId,
            item.ProductId,
            item.ProductName,
            item.SpecDescJson,
            item.MainImage,
            item.UnitPrice,
            item.Quantity,
            item.Selected,
            item.UpdatedAt
        )).ToList();

        var selectedTotal = dtos.Where(x => x.Selected).Sum(x => x.UnitPrice * x.Quantity);
        return new CartDto(userId, dtos, selectedTotal);
    }

    public async Task AddItemAsync(long userId, CartItemRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            throw new BusinessException("INVALID_QUANTITY", "数量必须大于0");

        // 1. 通过 Service 接口查询 SKU 信息
        var sku = await _skuService.GetByIdAsync(request.SkuId, cancellationToken);
        if (sku == null)
            throw new BusinessException("SKU_NOT_FOUND", "SKU不存在");

        // 校验 SKU 是否在售（使用枚举）
        if (sku.Status != (int)SkuStatus.Enabled)
            throw new BusinessException("SKU_NOT_AVAILABLE", "SKU已停售");
        if (sku.ProductStatus is not ((int)ProductStatus.OnShelf or (int)ProductStatus.Presale))
            throw new BusinessException("PRODUCT_OFF_SHELF", "商品已下架");

        // 2. 校验库存（可用库存 = stock - locked_stock）
        var availableStock = sku.Stock - sku.LockedStock;
        if (availableStock < request.Quantity)
            throw new BusinessException("INSUFFICIENT_STOCK", $"库存不足，当前可用库存：{availableStock}");

        // 3. 原子累加已有购物车项，避免“查询后更新”导致并发丢失。
        if (await _cartRepository.TryIncreaseQuantityAsync(
                userId,
                request.SkuId,
                request.Quantity,
                availableStock,
                DateTime.Now,
                cancellationToken) == 1)
        {
            return;
        }

        var cart = new Cart
        {
            UserId = userId,
            SkuId = request.SkuId,
            Quantity = request.Quantity,
            Selected = 1,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        try
        {
            await _cartRepository.AddAsync(cart, cancellationToken);
            return;
        }
        catch (OracleException ex) when (ex.Number == 1)
        {
            // 两个请求同时首次加入同一 SKU 时，唯一键竞争的失败方重试原子累加。
            if (await _cartRepository.TryIncreaseQuantityAsync(
                    userId,
                    request.SkuId,
                    request.Quantity,
                    availableStock,
                    DateTime.Now,
                    cancellationToken) == 1)
            {
                return;
            }
        }

        var existing = await _cartRepository.GetByUserAndSkuAsync(userId, request.SkuId, cancellationToken);
        if (existing != null)
        {
            throw new BusinessException("INSUFFICIENT_STOCK", $"购物车中已有 {existing.Quantity} 件，再添加 {request.Quantity} 件将超过库存");
        }

        throw new BusinessException("CART_ADD_FAILED", "加入购物车失败，请稍后重试");
    }

    public async Task UpdateItemAsync(long userId, long cartItemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
    {
        // 校验该购物车项是否属于当前用户
        var items = await _cartRepository.GetUserCartWithDetailsAsync(userId, cancellationToken);
        var target = items.FirstOrDefault(x => x.CartItemId == cartItemId);
        if (target == null)
            throw new BusinessException("CART_ITEM_NOT_FOUND", "购物车项不存在或不属于您");

        // 修改数量必须 > 0
        if (request.Quantity <= 0)
            throw new BusinessException("INVALID_QUANTITY", "数量必须大于0");

        // 校验库存
        var sku = await _skuService.GetByIdAsync(target.SkuId, cancellationToken);
        if (sku == null)
            throw new BusinessException("SKU_NOT_FOUND", "SKU不存在");

        // 校验 SKU 是否在售（使用枚举）
        if (sku.Status != (int)SkuStatus.Enabled)
            throw new BusinessException("SKU_NOT_AVAILABLE", "SKU已停售");
        if (sku.ProductStatus is not ((int)ProductStatus.OnShelf or (int)ProductStatus.Presale))
            throw new BusinessException("PRODUCT_OFF_SHELF", "商品已下架");

        var availableStock = sku.Stock - sku.LockedStock;
        if (availableStock < request.Quantity)
            throw new BusinessException("INSUFFICIENT_STOCK", $"库存不足，当前可用库存：{availableStock}");

        // 更新
        var cart = new Cart
        {
            Id = cartItemId,
            UserId = userId,
            SkuId = target.SkuId,
            Quantity = request.Quantity,
            Selected = request.Selected ? 1 : 0,
            CreatedAt = target.UpdatedAt,
            UpdatedAt = DateTime.Now
        };
        await _cartRepository.UpdateAsync(cart, cancellationToken);
    }

    public async Task RemoveItemAsync(long userId, long cartItemId, CancellationToken cancellationToken = default)
    {
        var items = await _cartRepository.GetUserCartWithDetailsAsync(userId, cancellationToken);
        if (!items.Any(x => x.CartItemId == cartItemId))
            throw new BusinessException("CART_ITEM_NOT_FOUND", "购物车项不存在或不属于您");

        await _cartRepository.RemoveAsync(cartItemId, cancellationToken);
    }

    public async Task ClearAsync(long userId, CancellationToken cancellationToken = default)
    {
        // 一次性清空，避免循环删除
        await _cartRepository.ClearAllAsync(userId, cancellationToken);
    }
}
