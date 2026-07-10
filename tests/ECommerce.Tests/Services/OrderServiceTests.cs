using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Models;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;
using ECommerce.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services;

public class OrderServiceTests : ServiceTestBase
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<ICartRepository> _cartRepositoryMock;
    private readonly Mock<ISkuService> _skuServiceMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAddressService> _addressServiceMock;
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly Mock<IOperationLogService> _operationLogServiceMock;
    private readonly Mock<ILogger<OrderService>> _loggerMock;
    private readonly OrderService _orderService;

    private const long TestUserId = 1;
    private const long TestOrderId = 1;
    private const long TestSkuId = 100;

    public OrderServiceTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _orderRepositoryMock
            .Setup(x => x.TryUpdateStatusAsync(
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _cartRepositoryMock = new Mock<ICartRepository>();
        _skuServiceMock = new Mock<ISkuService>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = CreateUnitOfWorkMock();
        _addressServiceMock = new Mock<IAddressService>();
        _inventoryServiceMock = new Mock<IInventoryService>();
        _operationLogServiceMock = new Mock<IOperationLogService>();
        _loggerMock = CreateLoggerMock<OrderService>();

        _orderService = new OrderService(
            _orderRepositoryMock.Object,
            _cartRepositoryMock.Object,
            _skuServiceMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _addressServiceMock.Object,
            _inventoryServiceMock.Object,
            _operationLogServiceMock.Object,
            _loggerMock.Object
        );
    }

    #region PreviewAsync Tests

    [Fact]
    public async Task PreviewAsync_ShouldReturnPreview()
    {
        // Arrange
        var request = new CreateOrderRequest(
            AddressId: 1,
            UserCouponId: null,
            CartItemIds: new List<long> { 1, 2 },
            Remark: null
        );

        var cartItems = new List<CartItemWithDetails>
        {
            CreateCartItemWithDetails(cartItemId: 1, skuId: 100, unitPrice: 99.99m, quantity: 2),
            CreateCartItemWithDetails(cartItemId: 2, skuId: 101, unitPrice: 59.99m, quantity: 1)
        };

        var sku = CreateSkuDto(skuId: 100, stock: 100);
        var sku2 = CreateSkuDto(skuId: 101, stock: 50);

        SetupAddressService(TestUserId, request.AddressId);

        _cartRepositoryMock
            .Setup(x => x.GetUserCartWithDetailsAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartItems);

        _skuServiceMock
            .Setup(x => x.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sku);
        _skuServiceMock
            .Setup(x => x.GetByIdAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sku2);

        // Act
        var result = await _orderService.PreviewAsync(TestUserId, request);

        // Assert
        Assert.Equal(259.97m, result.TotalAmount); // 99.99*2 + 59.99*1
        Assert.Equal(0, result.DiscountAmount);
        Assert.Equal(259.97m, result.PayAmount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task PreviewAsync_WithCoupon_ShouldReportNotReady()
    {
        // Arrange
        var request = new CreateOrderRequest(
            AddressId: 1,
            UserCouponId: 1001,
            CartItemIds: new List<long> { 1 },
            Remark: null
        );

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.PreviewAsync(TestUserId, request));

        Assert.Equal("COUPON_NOT_READY", exception.Code);
        _cartRepositoryMock.Verify(x => x.GetUserCartWithDetailsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PreviewAsync_CartEmpty_ShouldThrow()
    {
        // Arrange
        var request = new CreateOrderRequest(
            AddressId: 1,
            UserCouponId: null,
            CartItemIds: null,
            Remark: null
        );

        SetupAddressService(TestUserId, request.AddressId);

        _cartRepositoryMock
            .Setup(x => x.GetUserCartWithDetailsAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CartItemWithDetails>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.PreviewAsync(TestUserId, request)
        );
        Assert.Equal("CART_EMPTY", exception.Code);
    }

    [Fact]
    public async Task PreviewAsync_SkuOutOfStock_ShouldThrow()
    {
        // Arrange
        var request = new CreateOrderRequest(
            AddressId: 1,
            UserCouponId: null,
            CartItemIds: new List<long> { 1 },
            Remark: null
        );

        var cartItems = new List<CartItemWithDetails>
        {
            CreateCartItemWithDetails(cartItemId: 1, skuId: 100, quantity: 5) // 需要 5 件
        };

        var sku = CreateSkuDto(skuId: 100, stock: 10, lockedStock: 8); // 可用库存 = 2

        SetupAddressService(TestUserId, request.AddressId);

        _cartRepositoryMock
            .Setup(x => x.GetUserCartWithDetailsAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartItems);

        _skuServiceMock
            .Setup(x => x.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sku);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.PreviewAsync(TestUserId, request)
        );
        Assert.Equal("INSUFFICIENT_STOCK", exception.Code);
        Assert.Contains("库存不足", exception.Message);
    }

    [Fact]
    public async Task PreviewAsync_AddressNotFound_ShouldThrow()
    {
        // Arrange
        var request = new CreateOrderRequest(
            AddressId: 999,
            UserCouponId: null,
            CartItemIds: new List<long> { 1 },
            Remark: null
        );

        _addressServiceMock
            .Setup(x => x.GetMyAddressesAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AddressDto>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.PreviewAsync(TestUserId, request)
        );
        Assert.Equal("ADDRESS_NOT_FOUND", exception.Code);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ShouldCreateOrderSuccessfully()
    {
        // Arrange
        var request = new CreateOrderRequest(
            AddressId: 1,
            UserCouponId: null,
            CartItemIds: new List<long> { 1 },
            Remark: "请尽快发货"
        );

        var address = CreateAddressDto(addressId: 1);
        var cartItems = new List<CartItemWithDetails>
        {
            CreateCartItemWithDetails(cartItemId: 1, skuId: 100, unitPrice: 99.99m, quantity: 2)
        };
        var sku = CreateSkuDto(skuId: 100, stock: 100);

        SetupAddressService(TestUserId, request.AddressId, address);

        _cartRepositoryMock
            .Setup(x => x.GetUserCartWithDetailsAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartItems);

        _skuServiceMock
            .Setup(x => x.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sku);

        _orderRepositoryMock
            .Setup(x => x.InsertOrderMainAsync(It.IsAny<OrderMain>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestOrderId);

        _orderRepositoryMock
            .Setup(x => x.InsertOrderItemsAsync(It.IsAny<IEnumerable<OrderItem>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _orderRepositoryMock
            .Setup(x => x.InsertOrderLogAsync(It.IsAny<OrderLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _inventoryServiceMock
            .Setup(x => x.LockForOrderAsync(TestOrderId, It.IsAny<IReadOnlyList<OrderSkuQuantity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cartRepositoryMock
            .Setup(x => x.ClearByIdsAsync(TestUserId, It.Is<IReadOnlyList<long>>(ids => ids.SequenceEqual(new long[] { 1 })), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var orderId = await _orderService.CreateAsync(TestUserId, request);

        // Assert
        Assert.Equal(TestOrderId, orderId);

        // 验证事务流程
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        // 验证核心操作被调用
        _orderRepositoryMock.Verify(x => x.InsertOrderMainAsync(It.Is<OrderMain>(o =>
            o.UserId == TestUserId &&
            o.Status == (int)OrderStatus.PendingPayment
        ), It.IsAny<CancellationToken>()), Times.Once);

        _inventoryServiceMock.Verify(x => x.LockForOrderAsync(TestOrderId, It.IsAny<IReadOnlyList<OrderSkuQuantity>>(), It.IsAny<CancellationToken>()), Times.Once);

        _cartRepositoryMock.Verify(x => x.ClearByIdsAsync(
            TestUserId,
            It.Is<IReadOnlyList<long>>(ids => ids.SequenceEqual(new long[] { 1 })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithCoupon_ShouldReportNotReady()
    {
        // Arrange
        var request = new CreateOrderRequest(
            AddressId: 1,
            UserCouponId: 1001,
            CartItemIds: new List<long> { 1 },
            Remark: null
        );

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.CreateAsync(TestUserId, request));

        Assert.Equal("COUPON_NOT_READY", exception.Code);
        _orderRepositoryMock.Verify(x => x.InsertOrderMainAsync(It.IsAny<OrderMain>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InventoryLockFails_ShouldRollback()
    {
        // Arrange
        var request = new CreateOrderRequest(
            AddressId: 1,
            UserCouponId: null,
            CartItemIds: new List<long> { 1 },
            Remark: null
        );

        var address = CreateAddressDto(addressId: 1);
        var cartItems = new List<CartItemWithDetails>
        {
            CreateCartItemWithDetails(cartItemId: 1, skuId: 100, unitPrice: 99.99m, quantity: 2)
        };
        var sku = CreateSkuDto(skuId: 100, stock: 100);

        SetupAddressService(TestUserId, request.AddressId, address);

        _cartRepositoryMock
            .Setup(x => x.GetUserCartWithDetailsAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartItems);

        _skuServiceMock
            .Setup(x => x.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sku);

        _orderRepositoryMock
            .Setup(x => x.InsertOrderMainAsync(It.IsAny<OrderMain>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestOrderId);

        _orderRepositoryMock
            .Setup(x => x.InsertOrderItemsAsync(It.IsAny<IEnumerable<OrderItem>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _orderRepositoryMock
            .Setup(x => x.InsertOrderLogAsync(It.IsAny<OrderLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // 模拟库存锁定失败
        _inventoryServiceMock
            .Setup(x => x.LockForOrderAsync(TestOrderId, It.IsAny<IReadOnlyList<OrderSkuQuantity>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessException("STOCK_LOCK_FAILED", "锁定库存失败"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.CreateAsync(TestUserId, request)
        );
        Assert.Equal("STOCK_LOCK_FAILED", exception.Code);

        // 验证回滚被调用
        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);

        // 验证提交未被调用
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region CancelAsync Tests

    [Fact]
    public async Task CancelAsync_UserSelfCancel_ShouldCancelWithoutOperationLog()
    {
        // Arrange
        var order = CreateOrderMain(orderId: TestOrderId, userId: TestUserId, status: (int)OrderStatus.PendingPayment);
        var skuQuantities = new List<OrderSkuQuantity>
        {
            new(TestSkuId, 2)
        };

        SetupOrderForCancel(order, skuQuantities);

        // Act
        await _orderService.CancelAsync(
            userId: TestUserId,
            orderId: TestOrderId,
            operatorId: TestUserId, // 操作人 = 订单主人
            operatorName: "testuser",
            reason: "不想买了",
            ipAddress: "127.0.0.1"
        );

        // Assert
        _orderRepositoryMock.Verify(x => x.TryUpdateStatusAsync(
            TestOrderId,
            (int)OrderStatus.PendingPayment,
            (int)OrderStatus.Cancelled,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _orderRepositoryMock.Verify(x => x.InsertOrderLogAsync(It.Is<OrderLog>(log =>
            log.ToStatus == (int)OrderStatus.Cancelled &&
            log.Remark == "不想买了"
        ), It.IsAny<CancellationToken>()), Times.Once);

        _inventoryServiceMock.Verify(x => x.ReleaseForCancelledOrderAsync(
            TestOrderId,
            It.IsAny<IReadOnlyList<OrderSkuQuantity>>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        // 操作人是订单主人 → 不写入 OPERATION_LOG
        _operationLogServiceMock.Verify(
            x => x.WriteAsync(It.IsAny<OperationLogRequest>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_AdminCancel_ShouldCancelWithOperationLog()
    {
        // Arrange
        var order = CreateOrderMain(orderId: TestOrderId, userId: TestUserId, status: (int)OrderStatus.PendingPayment);
        var skuQuantities = new List<OrderSkuQuantity>
        {
            new(TestSkuId, 2)
        };

        SetupOrderForCancel(order, skuQuantities);

        // Act
        await _orderService.CancelAsync(
            userId: TestUserId,
            orderId: TestOrderId,
            operatorId: 999, // 管理员 ID，不同于订单主人
            operatorName: "admin",
            reason: "违规订单",
            ipAddress: "192.168.1.100"
        );

        // Assert
        _orderRepositoryMock.Verify(x => x.TryUpdateStatusAsync(
            TestOrderId,
            (int)OrderStatus.PendingPayment,
            (int)OrderStatus.Cancelled,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _orderRepositoryMock.Verify(x => x.InsertOrderLogAsync(It.Is<OrderLog>(log =>
            log.ToStatus == (int)OrderStatus.Cancelled &&
            log.OperatorId == 999 &&
            log.Remark == "违规订单"
        ), It.IsAny<CancellationToken>()), Times.Once);

        _inventoryServiceMock.Verify(x => x.ReleaseForCancelledOrderAsync(
            TestOrderId,
            It.IsAny<IReadOnlyList<OrderSkuQuantity>>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        // 操作人不是订单主人 → 写入 OPERATION_LOG
        _operationLogServiceMock.Verify(x => x.WriteAsync(It.Is<OperationLogRequest>(log =>
            log.OperatorId == 999 &&
            log.OperatorName == "admin" &&
            log.Module == "订单管理" &&
            log.Action == "后台取消订单" &&
            log.IpAddress == "192.168.1.100"
        ), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_InvalidStatus_ShouldThrow()
    {
        // Arrange
        var order = CreateOrderMain(orderId: TestOrderId, userId: TestUserId, status: (int)OrderStatus.Paid);

        _orderRepositoryMock
            .Setup(x => x.GetOrderByIdAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.CancelAsync(
                userId: TestUserId,
                orderId: TestOrderId,
                operatorId: TestUserId,
                operatorName: "testuser",
                reason: "test",
                ipAddress: "127.0.0.1"
            )
        );
        Assert.Equal("ORDER_CANNOT_CANCEL", exception.Code);

        // 验证事务未被使用
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_WrongUser_ShouldThrow()
    {
        // Arrange
        var order = CreateOrderMain(orderId: TestOrderId, userId: 999, status: (int)OrderStatus.PendingPayment);

        _orderRepositoryMock
            .Setup(x => x.GetOrderByIdAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.CancelAsync(
                userId: TestUserId,
                orderId: TestOrderId,
                operatorId: TestUserId,
                operatorName: "testuser",
                reason: "test",
                ipAddress: "127.0.0.1"
            )
        );
        Assert.Equal("FORBIDDEN", exception.Code);

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _orderRepositoryMock.Verify(x => x.TryUpdateStatusAsync(
            It.IsAny<long>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()
        ), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_OrderNotFound_ShouldThrow()
    {
        // Arrange
        _orderRepositoryMock
            .Setup(x => x.GetOrderByIdAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderMain?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.CancelAsync(
                userId: TestUserId,
                orderId: TestOrderId,
                operatorId: TestUserId,
                operatorName: "testuser",
                reason: "test",
                ipAddress: "127.0.0.1"
            )
        );
        Assert.Equal("ORDER_NOT_FOUND", exception.Code);
    }

    #endregion

    #region ConfirmAsync Tests

    [Fact]
    public async Task ConfirmAsync_ShouldConfirmSuccessfully()
    {
        // Arrange
        var order = CreateOrderMain(orderId: TestOrderId, userId: TestUserId, status: (int)OrderStatus.Shipped);

        _orderRepositoryMock
            .Setup(x => x.GetOrderByIdAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _orderRepositoryMock
            .Setup(x => x.TryUpdateStatusAsync(TestOrderId, (int)OrderStatus.Shipped, (int)OrderStatus.Completed, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _orderRepositoryMock
            .Setup(x => x.InsertOrderLogAsync(It.IsAny<OrderLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _orderService.ConfirmAsync(TestUserId, TestOrderId);

        // Assert
        _orderRepositoryMock.Verify(x => x.TryUpdateStatusAsync(
            TestOrderId,
            (int)OrderStatus.Shipped,
            (int)OrderStatus.Completed,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _orderRepositoryMock.Verify(x => x.InsertOrderLogAsync(It.Is<OrderLog>(log =>
            log.FromStatus == (int)OrderStatus.Shipped &&
            log.ToStatus == (int)OrderStatus.Completed &&
            log.OperatorId == TestUserId &&
            log.Remark == "用户确认收货"
        ), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmAsync_InvalidStatus_ShouldThrow()
    {
        // Arrange
        var order = CreateOrderMain(orderId: TestOrderId, userId: TestUserId, status: (int)OrderStatus.PendingPayment);

        _orderRepositoryMock
            .Setup(x => x.GetOrderByIdAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.ConfirmAsync(TestUserId, TestOrderId)
        );
        Assert.Equal("ORDER_CANNOT_CONFIRM", exception.Code);
    }

    [Fact]
    public async Task ConfirmAsync_WrongUser_ShouldThrow()
    {
        // Arrange
        var order = CreateOrderMain(orderId: TestOrderId, userId: 999, status: (int)OrderStatus.Shipped);

        _orderRepositoryMock
            .Setup(x => x.GetOrderByIdAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.ConfirmAsync(TestUserId, TestOrderId)
        );
        Assert.Equal("FORBIDDEN", exception.Code);
    }

    #endregion

    #region MarkPaidAsync Tests

    [Fact]
    public async Task MarkPaidAsync_ShouldMarkPaidSuccessfully()
    {
        // Arrange
        var order = CreateOrderMain(orderId: TestOrderId, userId: TestUserId, status: (int)OrderStatus.PendingPayment);

        var skuQuantities = new List<OrderSkuQuantity>
        {
            new(TestSkuId, 2)
        };

        _orderRepositoryMock
            .Setup(x => x.GetOrderByIdAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _orderRepositoryMock
            .Setup(x => x.TryUpdateStatusAsync(TestOrderId, (int)OrderStatus.PendingPayment, (int)OrderStatus.Paid, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _orderRepositoryMock
            .Setup(x => x.InsertOrderLogAsync(It.IsAny<OrderLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _orderRepositoryMock
            .Setup(x => x.GetOrderSkuQuantitiesAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(skuQuantities);

        _inventoryServiceMock
            .Setup(x => x.DeductForPaidOrderAsync(TestOrderId, skuQuantities, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _skuServiceMock
            .Setup(x => x.GetByIdAsync(TestSkuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSkuDto(TestSkuId));
        _productRepositoryMock
            .Setup(x => x.IncrementSalesCountAsync(10, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _orderService.MarkPaidAsync(TestOrderId, 5001);

        // Assert
        _orderRepositoryMock.Verify(x => x.TryUpdateStatusAsync(
            TestOrderId,
            (int)OrderStatus.PendingPayment,
            (int)OrderStatus.Paid,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _inventoryServiceMock.Verify(x => x.DeductForPaidOrderAsync(
            TestOrderId,
            skuQuantities,
            It.IsAny<CancellationToken>()
        ), Times.Once);
        _productRepositoryMock.Verify(x => x.IncrementSalesCountAsync(10, 2, It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkPaidAsync_AlreadyPaid_ShouldBeIdempotent()
    {
        // Arrange
        var order = CreateOrderMain(orderId: TestOrderId, userId: TestUserId, status: (int)OrderStatus.Paid);

        _orderRepositoryMock
            .Setup(x => x.GetOrderByIdAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        await _orderService.MarkPaidAsync(TestOrderId, 5001);

        // Assert
        // 不应该重复更新状态
        _orderRepositoryMock.Verify(
            x => x.TryUpdateStatusAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _inventoryServiceMock.Verify(
            x => x.DeductForPaidOrderAsync(It.IsAny<long>(), It.IsAny<IReadOnlyList<OrderSkuQuantity>>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkPaidAsync_InvalidStatus_ShouldThrow()
    {
        // Arrange
        var order = CreateOrderMain(orderId: TestOrderId, userId: TestUserId, status: (int)OrderStatus.Cancelled);

        _orderRepositoryMock
            .Setup(x => x.GetOrderByIdAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.MarkPaidAsync(TestOrderId, 5001)
        );
        Assert.Equal("ORDER_STATUS_INVALID", exception.Code);
    }

    [Fact]
    public async Task MarkPaidAsync_OrderWithCoupon_ShouldReportNotReady()
    {
        var order = CreateOrderMain(orderId: TestOrderId, userId: TestUserId, status: (int)OrderStatus.PendingPayment);
        order.UserCouponId = 1001;

        _orderRepositoryMock
            .Setup(x => x.GetOrderByIdAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.MarkPaidAsync(TestOrderId, 5001));

        Assert.Equal("COUPON_NOT_READY", exception.Code);
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkPaidAsync_WhenStatusChangesConcurrently_ShouldRollbackWithoutDeductingStock()
    {
        var order = CreateOrderMain(orderId: TestOrderId, userId: TestUserId, status: (int)OrderStatus.PendingPayment);
        _orderRepositoryMock
            .Setup(x => x.GetOrderByIdAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _orderRepositoryMock
            .Setup(x => x.TryUpdateStatusAsync(
                TestOrderId,
                (int)OrderStatus.PendingPayment,
                (int)OrderStatus.Paid,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.MarkPaidAsync(TestOrderId, 5001));

        Assert.Equal("ORDER_STATUS_CHANGED", exception.Code);
        _inventoryServiceMock.Verify(
            x => x.DeductForPaidOrderAsync(It.IsAny<long>(), It.IsAny<IReadOnlyList<OrderSkuQuantity>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region MarkShippedAsync Tests

    [Fact]
    public async Task MarkShippedAsync_ShouldMarkShippedSuccessfully()
    {
        // Arrange
        var order = CreateOrderMain(orderId: TestOrderId, userId: TestUserId, status: (int)OrderStatus.Paid);

        _orderRepositoryMock
            .Setup(x => x.GetOrderByIdAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _orderRepositoryMock
            .Setup(x => x.TryUpdateStatusAsync(TestOrderId, (int)OrderStatus.Paid, (int)OrderStatus.Shipped, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _orderRepositoryMock
            .Setup(x => x.InsertOrderLogAsync(It.IsAny<OrderLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _operationLogServiceMock
            .Setup(x => x.WriteAsync(It.IsAny<OperationLogRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _orderService.MarkShippedAsync(
            orderId: TestOrderId,
            logisticsId: 100,
            operatorId: 999,
            operatorName: "admin",
            ipAddress: "192.168.1.100"
        );

        // Assert
        _orderRepositoryMock.Verify(x => x.TryUpdateStatusAsync(
            TestOrderId,
            (int)OrderStatus.Paid,
            (int)OrderStatus.Shipped,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _orderRepositoryMock.Verify(x => x.InsertOrderLogAsync(It.Is<OrderLog>(log =>
            log.FromStatus == (int)OrderStatus.Paid &&
            log.ToStatus == (int)OrderStatus.Shipped &&
            log.OperatorId == 999 &&
            log.Remark.Contains("物流ID：100")
        ), It.IsAny<CancellationToken>()), Times.Once);

        _operationLogServiceMock.Verify(x => x.WriteAsync(It.Is<OperationLogRequest>(log =>
            log.OperatorId == 999 &&
            log.OperatorName == "admin" &&
            log.Module == "订单管理" &&
            log.Action == "发货" &&
            log.IpAddress == "192.168.1.100"
        ), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkShippedAsync_InvalidStatus_ShouldThrow()
    {
        // Arrange
        var order = CreateOrderMain(orderId: TestOrderId, userId: TestUserId, status: (int)OrderStatus.PendingPayment);

        _orderRepositoryMock
            .Setup(x => x.GetOrderByIdAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.MarkShippedAsync(TestOrderId, 100, 999, "admin", "127.0.0.1")
        );
        Assert.Equal("ORDER_STATUS_INVALID", exception.Code);
    }

    #endregion

    #region SearchMineAsync Tests

    [Fact]
    public async Task SearchMineAsync_ShouldReturnUserOrders()
    {
        // Arrange
        var query = new OrderQuery();
        var expectedResult = new PagedResult<OrderListItemDto>(
            Items: new List<OrderListItemDto>
            {
                new(1, "OD001", TestUserId, 0, 199.98m, DateTime.Now, DateTime.Now)
            },
            PageIndex: 1,
            PageSize: 20,
            TotalCount: 1
        );

        _orderRepositoryMock
            .Setup(x => x.SearchUserOrdersAsync(TestUserId, query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _orderService.SearchMineAsync(TestUserId, query);

        // Assert
        Assert.Equal(expectedResult, result);
        _orderRepositoryMock.Verify(x => x.SearchUserOrdersAsync(TestUserId, query, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetDetailAsync Tests

    [Fact]
    public async Task GetDetailAsync_ShouldReturnOrderDetail()
    {
        // Arrange
        var order = CreateOrderMain(orderId: TestOrderId, userId: TestUserId);

        _orderRepositoryMock
            .Setup(x => x.GetFullOrderAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _orderService.GetDetailAsync(TestUserId, TestOrderId);

        // Assert
        Assert.Equal(TestOrderId, result.OrderId);
        Assert.Equal(TestUserId, result.UserId);
        Assert.Equal(order.Status, result.Status);
        Assert.Equal(1, result.Items.Count);
        Assert.Equal(1, result.Logs.Count);
    }

    [Fact]
    public async Task GetDetailAsync_WrongUser_ShouldThrow()
    {
        // Arrange
        var order = CreateOrderMain(orderId: TestOrderId, userId: 999);

        _orderRepositoryMock
            .Setup(x => x.GetFullOrderAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.GetDetailAsync(TestUserId, TestOrderId)
        );
        Assert.Equal("FORBIDDEN", exception.Code);
    }

    [Fact]
    public async Task GetDetailAsync_OrderNotFound_ShouldThrow()
    {
        // Arrange
        _orderRepositoryMock
            .Setup(x => x.GetFullOrderAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderMain?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _orderService.GetDetailAsync(TestUserId, TestOrderId)
        );
        Assert.Equal("ORDER_NOT_FOUND", exception.Code);
    }

    #endregion

    #region Helper Methods

    private void SetupAddressService(long userId, long addressId, AddressDto? address = null)
    {
        var addresses = new List<AddressDto> { address ?? CreateAddressDto(addressId: addressId) };
        _addressServiceMock
            .Setup(x => x.GetMyAddressesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(addresses);
    }

    private void SetupOrderForCancel(OrderMain order, IReadOnlyList<OrderSkuQuantity> skuQuantities)
    {
        _orderRepositoryMock
            .Setup(x => x.GetOrderByIdAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _orderRepositoryMock
            .Setup(x => x.TryUpdateStatusAsync(TestOrderId, (int)OrderStatus.PendingPayment, (int)OrderStatus.Cancelled, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _orderRepositoryMock
            .Setup(x => x.InsertOrderLogAsync(It.IsAny<OrderLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _orderRepositoryMock
            .Setup(x => x.GetOrderSkuQuantitiesAsync(TestOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(skuQuantities);

        _inventoryServiceMock
            .Setup(x => x.ReleaseForCancelledOrderAsync(TestOrderId, skuQuantities, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    #endregion
}
