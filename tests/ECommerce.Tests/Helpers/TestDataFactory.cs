using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Tests.Helpers;

public static class TestDataFactory
{
    public static Cart CreateCart(long userId = 1, long skuId = 100, int quantity = 2)
    {
        return new Cart
        {
            Id = 1,
            UserId = userId,
            SkuId = skuId,
            Quantity = quantity,
            Selected = 1,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    public static CartItemWithDetails CreateCartItemWithDetails(
        long cartItemId = 1,
        long skuId = 100,
        long productId = 10,
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
            ProductName = productName,
            SpecDescJson = "{\"颜色\":\"红色\",\"尺码\":\"M\"}",
            MainImage = "/images/test.jpg",
            UnitPrice = unitPrice,
            Quantity = quantity,
            Selected = selected,
            UpdatedAt = DateTime.Now
        };
    }

    public static SkuDto CreateSkuDto(
        long skuId = 100,
        int stock = 100,
        int lockedStock = 0,
        int status = 1,
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

    public static OrderMain CreateOrder(
        long orderId = 1,
        long userId = 1,
        int status = 0,
        decimal payAmount = 199.98m)
    {
        return new OrderMain
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
            ReceiverSnapshot = "{}",
            Remark = null,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            Items = new List<OrderItem>(),
            Logs = new List<OrderLog>()
        };
    }
}
