using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Models;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly ISkuService _skuService;
    private readonly IUnitOfWork _unitOfWork;

    public CartService(
        ICartRepository cartRepository,
        ISkuService skuService,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _skuService = skuService;
        _unitOfWork = unitOfWork;
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
        // 1. 通过 Service 接口查询 SKU 信息
        var sku = await _skuService.GetByIdAsync(request.SkuId, cancellationToken);
        if (sku == null)
            throw new BusinessException("SKU_NOT_FOUND", "SKU不存在");

        // 校验 SKU 是否在售（1=在售）
        if (sku.Status != 1)
            throw new BusinessException("SKU_NOT_AVAILABLE", "SKU已停售");

        // 注意：商品上下架状态由 SKU 的 Status 间接覆盖，不单独校验 Product.Status
        // 如果后续需要校验商品状态，可调用 IProductService

        // 2. 校验库存（可用库存 = stock - locked_stock）
        var availableStock = sku.Stock - sku.LockedStock;
        if (availableStock < request.Quantity)
            throw new BusinessException("INSUFFICIENT_STOCK", $"库存不足，当前可用库存：{availableStock}");

        // 3. 查询是否已在购物车中
        var existing = await _cartRepository.GetByUserAndSkuAsync(userId, request.SkuId, cancellationToken);
        if (existing != null)
        {
            // 累加数量，但不超过库存
            var newQuantity = existing.Quantity + request.Quantity;
            if (newQuantity > availableStock)
                throw new BusinessException("INSUFFICIENT_STOCK", $"购物车中已有 {existing.Quantity} 件，再添加 {request.Quantity} 件将超过库存");
            existing.Quantity = newQuantity;
            existing.UpdatedAt = DateTime.Now;
            await _cartRepository.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            var cart = new Cart
            {
                UserId = userId,
                SkuId = request.SkuId,
                Quantity = request.Quantity,
                Selected = 1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            await _cartRepository.AddAsync(cart, cancellationToken);
        }
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
        var items = await _cartRepository.GetUserCartWithDetailsAsync(userId, cancellationToken);
        foreach (var item in items)
        {
            await _cartRepository.RemoveAsync(item.CartItemId, cancellationToken);
        }
    }
}
