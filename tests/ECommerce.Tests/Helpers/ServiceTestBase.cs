using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Models;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Helpers;

/// <summary>
/// 测试基类，提供常用的 Mock 对象创建方法
/// </summary>
public abstract class ServiceTestBase
{
    protected Mock<IUnitOfWork> CreateUnitOfWorkMock()
    {
        var mock = new Mock<IUnitOfWork>();
        mock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    protected Mock<ILogger<T>> CreateLoggerMock<T>()
    {
        return new Mock<ILogger<T>>();
    }

    protected CartItemWithDetails CreateCartItemWithDetails(
        long cartItemId = 1,
        long skuId = 100,
        long productId = 10,
        int categoryId = 1,
        string productName = "测试商品",
        decimal unitPrice = 99.99m,
        int quantity = 2,
        bool selected = true)
    {
        return new CartItemWithDetails
        {
            CartItemId = cartItemId,
            SkuId = skuId,
            ProductId = productId,
            CategoryId = categoryId,
            ProductName = productName,
            SpecDescJson = "{\"颜色\":\"红色\",\"尺码\":\"M\"}",
            MainImage = "/images/test.jpg",
            UnitPrice = unitPrice,
            Quantity = quantity,
            Selected = selected,
            UpdatedAt = DateTime.Now
        };
    }

    protected SkuDto CreateSkuDto(
        long skuId = 100,
        int stock = 100,
        int lockedStock = 0,
        int status = (int)SkuStatus.Enabled,
        decimal price = 99.99m)
    {
        return new SkuDto(
            SkuId: skuId,
            ProductId: 10,
            SpecDescJson: "{\"颜色\":\"红色\",\"尺码\":\"M\"}",
            Price: price,
            OriginalPrice: 129.99m,
            Stock: stock,
            LockedStock: lockedStock,
            WarningStock: 10,
            SkuImage: "/images/sku.jpg",
            Status: status
        );
    }

    protected OrderMain CreateOrderMain(
        long orderId = 1,
        long userId = 1,
        int status = (int)OrderStatus.PendingPayment,
        decimal payAmount = 199.98m)
    {
        var order = new OrderMain
        {
            Id = orderId,
            OrderNo = "OD202601011200001234",
            UserId = userId,
            AddressId = 1,
            UserCouponId = null,
            Status = status,
            TotalAmount = 199.98m,
            DiscountAmount = 0,
            PayAmount = payAmount,
            PayExpireTime = DateTime.Now.AddMinutes(30),
            ReceiverSnapshot = "{\"ReceiverName\":\"张三\",\"ReceiverPhone\":\"13800001111\"}",
            Remark = null,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            Items = new List<OrderItem>(),
            Logs = new List<OrderLog>()
        };

        order.Items.Add(new OrderItem
        {
            Id = 1,
            OrderId = orderId,
            SkuId = 100,
            ProductNameSnap = "测试商品",
            SpecSnap = "红色/M",
            MainImageSnap = "/images/test.jpg",
            UnitPrice = 99.99m,
            Quantity = 2,
            Subtotal = 199.98m
        });

        order.Logs.Add(new OrderLog
        {
            Id = 1,
            OrderId = orderId,
            FromStatus = null,
            ToStatus = (int)OrderStatus.PendingPayment,
            OperatorId = null,
            Remark = "用户创建订单",
            CreatedAt = DateTime.Now
        });

        return order;
    }

    protected AddressDto CreateAddressDto(
        long addressId = 1,
        long userId = 1,
        bool isDefault = true)
    {
        return new AddressDto(
            AddressId: addressId,
            ReceiverName: "张三",
            ReceiverPhone: "13800001111",
            Province: "广东省",
            City: "深圳市",
            District: "南山区",
            DetailAddress: "科技园南区XX大厦A座1001",
            IsDefault: isDefault,
            CreatedAt: DateTime.Now
        );
    }

    protected CouponValidationDto CreateValidCouponValidation(decimal discountAmount = 20.00m)
    {
        return new CouponValidationDto(
            Available: true,
            DiscountAmount: discountAmount,
            Reason: null
        );
    }

    protected CouponValidationDto CreateInvalidCouponValidation(string reason = "优惠券已过期")
    {
        return new CouponValidationDto(
            Available: false,
            DiscountAmount: 0,
            Reason: reason
        );
    }
}
