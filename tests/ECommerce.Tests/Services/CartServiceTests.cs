using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Models;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Exceptions;
using ECommerce.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services;

public class CartServiceTests : ServiceTestBase
{
    private readonly Mock<ICartRepository> _cartRepositoryMock;
    private readonly Mock<ISkuService> _skuServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<CartService>> _loggerMock;
    private readonly CartService _cartService;

    public CartServiceTests()
    {
        _cartRepositoryMock = new Mock<ICartRepository>();
        _skuServiceMock = new Mock<ISkuService>();
        _unitOfWorkMock = CreateUnitOfWorkMock();
        _loggerMock = CreateLoggerMock<CartService>();

        _cartService = new CartService(
            _cartRepositoryMock.Object,
            _skuServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task GetCartAsync_ShouldReturnCartDto()
    {
        // Arrange
        var userId = 1L;
        var cartItems = new List<CartItemWithDetails>
        {
            CreateCartItemWithDetails(cartItemId: 1, skuId: 100, quantity: 2, selected: true),
            CreateCartItemWithDetails(cartItemId: 2, skuId: 101, quantity: 1, selected: false)
        };

        _cartRepositoryMock
            .Setup(x => x.GetUserCartWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartItems);

        // Act
        var result = await _cartService.GetCartAsync(userId);

        // Assert
        Assert.Equal(userId, result.UserId);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Items.Count(x => x.Selected));
        Assert.Equal(99.99m * 2, result.SelectedTotalAmount);
    }

    [Fact]
    public async Task AddItemAsync_NewItem_ShouldAddSuccessfully()
    {
        // Arrange
        var userId = 1L;
        var request = new CartItemRequest(SkuId: 100, Quantity: 2);
        var sku = CreateSkuDto(skuId: 100, stock: 100, lockedStock: 0);

        _skuServiceMock
            .Setup(x => x.GetByIdAsync(request.SkuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sku);

        _cartRepositoryMock
            .Setup(x => x.GetByUserAndSkuAsync(userId, request.SkuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        // Act
        await _cartService.AddItemAsync(userId, request);

        // Assert
        _cartRepositoryMock.Verify(x => x.AddAsync(It.Is<Cart>(c =>
            c.UserId == userId &&
            c.SkuId == request.SkuId &&
            c.Quantity == request.Quantity &&
            c.Selected == 1
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AddItemAsync_NonPositiveQuantity_ShouldThrow(int quantity)
    {
        var request = new CartItemRequest(SkuId: 100, Quantity: quantity);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _cartService.AddItemAsync(userId: 1, request));

        Assert.Equal("INVALID_QUANTITY", exception.Code);
        _skuServiceMock.Verify(x => x.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        _cartRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Cart>(), It.IsAny<CancellationToken>()), Times.Never);
        _cartRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Cart>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddItemAsync_ExistingItem_ShouldAtomicallyIncreaseQuantity()
    {
        // Arrange
        var userId = 1L;
        var request = new CartItemRequest(SkuId: 100, Quantity: 2);
        var sku = CreateSkuDto(skuId: 100, stock: 100, lockedStock: 0);
        _skuServiceMock
            .Setup(x => x.GetByIdAsync(request.SkuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sku);

        _cartRepositoryMock
            .Setup(x => x.TryIncreaseQuantityAsync(userId, request.SkuId, request.Quantity, 100, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _cartService.AddItemAsync(userId, request);

        // Assert
        _cartRepositoryMock.Verify(x => x.TryIncreaseQuantityAsync(
            userId, request.SkuId, request.Quantity, 100, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        _cartRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Cart>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddItemAsync_ExceedsAvailableStock_ShouldThrow()
    {
        // Arrange
        var userId = 1L;
        var request = new CartItemRequest(SkuId: 100, Quantity: 150);
        var sku = CreateSkuDto(skuId: 100, stock: 100, lockedStock: 0);

        _skuServiceMock
            .Setup(x => x.GetByIdAsync(request.SkuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sku);

        _cartRepositoryMock
            .Setup(x => x.GetByUserAndSkuAsync(userId, request.SkuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _cartService.AddItemAsync(userId, request)
        );
        Assert.Equal("INSUFFICIENT_STOCK", exception.Code);
        Assert.Contains("库存不足", exception.Message);
    }

    [Fact]
    public async Task AddItemAsync_SkuNotFound_ShouldThrow()
    {
        // Arrange
        var userId = 1L;
        var request = new CartItemRequest(SkuId: 999, Quantity: 2);

        _skuServiceMock
            .Setup(x => x.GetByIdAsync(request.SkuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SkuDto?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _cartService.AddItemAsync(userId, request)
        );
        Assert.Equal("SKU_NOT_FOUND", exception.Code);
    }

    [Fact]
    public async Task AddItemAsync_SkuDisabled_ShouldThrow()
    {
        // Arrange
        var userId = 1L;
        var request = new CartItemRequest(SkuId: 100, Quantity: 2);
        var sku = CreateSkuDto(skuId: 100, status: (int)SkuStatus.Disabled);

        _skuServiceMock
            .Setup(x => x.GetByIdAsync(request.SkuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sku);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _cartService.AddItemAsync(userId, request)
        );
        Assert.Equal("SKU_NOT_AVAILABLE", exception.Code);
    }

    [Fact]
    public async Task UpdateItemAsync_ShouldUpdateSuccessfully()
    {
        // Arrange
        var userId = 1L;
        var cartItemId = 1L;
        var request = new UpdateCartItemRequest(Quantity: 5, Selected: false);

        var cartItems = new List<CartItemWithDetails>
        {
            CreateCartItemWithDetails(cartItemId: cartItemId, skuId: 100, quantity: 2, selected: true)
        };

        var sku = CreateSkuDto(skuId: 100, stock: 100, lockedStock: 0);

        _cartRepositoryMock
            .Setup(x => x.GetUserCartWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartItems);

        _skuServiceMock
            .Setup(x => x.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sku);

        // Act
        await _cartService.UpdateItemAsync(userId, cartItemId, request);

        // Assert
        _cartRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Cart>(c =>
            c.Id == cartItemId &&
            c.Quantity == 5 &&
            c.Selected == 0
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateItemAsync_QuantityZero_ShouldThrow()
    {
        // Arrange
        var userId = 1L;
        var cartItemId = 1L;
        var request = new UpdateCartItemRequest(Quantity: 0, Selected: true);

        var cartItems = new List<CartItemWithDetails>
        {
            CreateCartItemWithDetails(cartItemId: cartItemId, skuId: 100, quantity: 2, selected: true)
        };

        _cartRepositoryMock
            .Setup(x => x.GetUserCartWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartItems);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _cartService.UpdateItemAsync(userId, cartItemId, request)
        );
        Assert.Equal("INVALID_QUANTITY", exception.Code);
    }

    [Fact]
    public async Task UpdateItemAsync_ItemNotFound_ShouldThrow()
    {
        // Arrange
        var userId = 1L;
        var cartItemId = 999L;
        var request = new UpdateCartItemRequest(Quantity: 5, Selected: true);

        _cartRepositoryMock
            .Setup(x => x.GetUserCartWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CartItemWithDetails>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _cartService.UpdateItemAsync(userId, cartItemId, request)
        );
        Assert.Equal("CART_ITEM_NOT_FOUND", exception.Code);
    }

    [Fact]
    public async Task RemoveItemAsync_ShouldRemoveSuccessfully()
    {
        // Arrange
        var userId = 1L;
        var cartItemId = 1L;

        var cartItems = new List<CartItemWithDetails>
        {
            CreateCartItemWithDetails(cartItemId: cartItemId, skuId: 100, quantity: 2, selected: true)
        };

        _cartRepositoryMock
            .Setup(x => x.GetUserCartWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartItems);

        // Act
        await _cartService.RemoveItemAsync(userId, cartItemId);

        // Assert
        _cartRepositoryMock.Verify(x => x.RemoveAsync(cartItemId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveItemAsync_ItemNotFound_ShouldThrow()
    {
        // Arrange
        var userId = 1L;
        var cartItemId = 999L;

        _cartRepositoryMock
            .Setup(x => x.GetUserCartWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CartItemWithDetails>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _cartService.RemoveItemAsync(userId, cartItemId)
        );
        Assert.Equal("CART_ITEM_NOT_FOUND", exception.Code);
    }

    [Fact]
    public async Task ClearAsync_ShouldClearAllItems()
    {
        // Arrange
        var userId = 1L;

        // Act
        await _cartService.ClearAsync(userId);

        // Assert
        _cartRepositoryMock.Verify(x => x.ClearAllAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
