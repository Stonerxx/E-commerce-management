using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Models;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;
using System.Text.Json;

namespace ECommerce.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly ISkuService _skuService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAddressService _addressService;
    private readonly IInventoryService _inventoryService;
    private readonly ICouponService _couponService;
    private readonly IOperationLogService _operationLogService;

    private const int PAY_EXPIRE_MINUTES = 30;

    public OrderService(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        ISkuService skuService,
        IUnitOfWork unitOfWork,
        IAddressService addressService,
        IInventoryService inventoryService,
        ICouponService couponService,
        IOperationLogService operationLogService)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _skuService = skuService;
        _unitOfWork = unitOfWork;
        _addressService = addressService;
        _inventoryService = inventoryService;
        _couponService = couponService;
        _operationLogService = operationLogService;
    }

    public async Task<OrderPreviewDto> PreviewAsync(long userId, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        // 1. 校验地址
        await ValidateAddressAsync(userId, request.AddressId, cancellationToken);

        // 2. 获取购物车选中项
        var cartItems = await GetSelectedCartItemsAsync(userId, request.CartItemIds, cancellationToken);
        if (cartItems.Count == 0)
            throw new BusinessException("CART_EMPTY", "购物车为空，请先添加商品");

        // 3. 校验每个 SKU 是否仍然有效（通过 Service 接口）
        foreach (var item in cartItems)
        {
            var sku = await _skuService.GetByIdAsync(item.SkuId, cancellationToken);
            if (sku == null)
                throw new BusinessException("SKU_NOT_FOUND", $"SKU {item.SkuId} 不存在");
            if (sku.Status != 1)
                throw new BusinessException("SKU_NOT_AVAILABLE", $"SKU {item.SkuId} 已停售");

            // 业务层快速校验：可用库存是否充足
            var availableStock = sku.Stock - sku.LockedStock;
            if (availableStock < item.Quantity)
                throw new BusinessException("INSUFFICIENT_STOCK", $"SKU {item.SkuId} 库存不足，当前可用：{availableStock}");
        }

        // 4. 计算金额
        decimal totalAmount = cartItems.Sum(x => x.UnitPrice * x.Quantity);
        decimal discountAmount = 0;

        // 5. 校验优惠券
        if (request.UserCouponId.HasValue)
        {
            var validation = await _couponService.ValidateAsync(userId, request.UserCouponId.Value, totalAmount, cancellationToken);
            if (!validation.Available)
                throw new BusinessException("COUPON_INVALID", validation.Reason ?? "优惠券不可用");
            discountAmount = validation.DiscountAmount;
        }

        var payAmount = totalAmount - discountAmount;
        if (payAmount < 0) payAmount = 0;

        // 6. 构造预览明细
        var items = cartItems.Select(x => new OrderItemDto(
            0,
            x.SkuId,
            x.ProductName,
            x.SpecDescJson,
            x.MainImage,
            x.UnitPrice,
            x.Quantity,
            x.UnitPrice * x.Quantity
        )).ToList();

        return new OrderPreviewDto(totalAmount, discountAmount, payAmount, items);
    }

    public async Task<long> CreateAsync(long userId, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        // 1. 前置校验
        var address = await ValidateAddressAsync(userId, request.AddressId, cancellationToken);

        var cartItems = await GetSelectedCartItemsAsync(userId, request.CartItemIds, cancellationToken);
        if (cartItems.Count == 0)
            throw new BusinessException("CART_EMPTY", "购物车为空，请先添加商品");

        // 2. 校验每个 SKU（通过 Service 接口）
        foreach (var item in cartItems)
        {
            var sku = await _skuService.GetByIdAsync(item.SkuId, cancellationToken);
            if (sku == null)
                throw new BusinessException("SKU_NOT_FOUND", $"SKU {item.SkuId} 不存在");
            if (sku.Status != 1)
                throw new BusinessException("SKU_NOT_AVAILABLE", $"SKU {item.SkuId} 已停售");

            var availableStock = sku.Stock - sku.LockedStock;
            if (availableStock < item.Quantity)
                throw new BusinessException("INSUFFICIENT_STOCK", $"SKU {item.SkuId} 库存不足，当前可用：{availableStock}");
        }

        // 3. 计算金额
        decimal totalAmount = cartItems.Sum(x => x.UnitPrice * x.Quantity);
        decimal discountAmount = 0;
        long? userCouponId = request.UserCouponId;

        if (userCouponId.HasValue)
        {
            var validation = await _couponService.ValidateAsync(userId, userCouponId.Value, totalAmount, cancellationToken);
            if (!validation.Available)
                throw new BusinessException("COUPON_INVALID", validation.Reason ?? "优惠券不可用");
            discountAmount = validation.DiscountAmount;
        }

        var payAmount = totalAmount - discountAmount;
        if (payAmount < 0) payAmount = 0;

        // 4. 生成订单编号
        var orderNo = GenerateOrderNo();

        // 5. 开启事务
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 6. 创建订单主表
            var order = new OrderMain
            {
                OrderNo = orderNo,
                UserId = userId,
                AddressId = request.AddressId,
                UserCouponId = userCouponId,
                Status = 0,
                TotalAmount = totalAmount,
                DiscountAmount = discountAmount,
                PayAmount = payAmount,
                PayExpireTime = DateTime.Now.AddMinutes(PAY_EXPIRE_MINUTES),
                ReceiverSnapshot = JsonSerializer.Serialize(new
                {
                    address.ReceiverName,
                    address.ReceiverPhone,
                    address.Province,
                    address.City,
                    address.District,
                    address.DetailAddress
                }),
                Remark = request.Remark,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var orderId = await _orderRepository.InsertOrderMainAsync(order, cancellationToken);

            // 7. 创建订单明细
            var items = cartItems.Select(x => new OrderItem
            {
                OrderId = orderId,
                SkuId = x.SkuId,
                ProductNameSnap = x.ProductName,
                SpecSnap = x.SpecDescJson,
                MainImageSnap = x.MainImage,
                UnitPrice = x.UnitPrice,
                Quantity = x.Quantity,
                Subtotal = x.UnitPrice * x.Quantity
            }).ToList();

            await _orderRepository.InsertOrderItemsAsync(items, cancellationToken);

            // 8. 记录订单日志
            await _orderRepository.InsertOrderLogAsync(new OrderLog
            {
                OrderId = orderId,
                FromStatus = null,
                ToStatus = 0,
                OperatorId = null,
                Remark = "用户创建订单",
                CreatedAt = DateTime.Now
            }, cancellationToken);

            // 9. 锁定库存（调用库存服务）
            var skuQuantities = items.Select(x => new OrderSkuQuantity(x.SkuId, x.Quantity)).ToList();
            await _inventoryService.LockForOrderAsync(orderId, skuQuantities, cancellationToken);

            // 10. 清空购物车（只清选中的）
            await _cartRepository.ClearSelectedAsync(userId, cancellationToken);

            // 11. 提交事务
            await _unitOfWork.CommitAsync(cancellationToken);

            return orderId;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PagedResult<OrderListItemDto>> SearchMineAsync(long userId, OrderQuery query, CancellationToken cancellationToken = default)
    {
        return await _orderRepository.SearchUserOrdersAsync(userId, query, cancellationToken);
    }

    public async Task<PagedResult<OrderListItemDto>> SearchAdminAsync(AdminOrderQuery query, CancellationToken cancellationToken = default)
    {
        return await _orderRepository.SearchAdminOrdersAsync(query, cancellationToken);
    }

    public async Task<OrderDetailDto> GetDetailAsync(long userId, long orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetFullOrderAsync(orderId, cancellationToken);
        if (order == null)
            throw new BusinessException("ORDER_NOT_FOUND", "订单不存在");

        if (order.UserId != userId)
            throw new BusinessException("FORBIDDEN", "无权查看此订单");

        return MapToOrderDetail(order);
    }

    public async Task<OrderDetailDto> GetAdminDetailAsync(long orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetFullOrderAsync(orderId, cancellationToken);
        if (order == null)
            throw new BusinessException("ORDER_NOT_FOUND", "订单不存在");

        return MapToOrderDetail(order);
    }

    public async Task<OrderPaymentContextDto> GetPaymentContextAsync(long userId, long orderId, CancellationToken cancellationToken = default)
    {
        var context = await _orderRepository.GetPaymentContextAsync(orderId, cancellationToken);
        if (context == null)
            throw new BusinessException("ORDER_NOT_FOUND", "订单不存在");

        if (context.UserId != userId)
            throw new BusinessException("FORBIDDEN", "无权查看此订单");

        return context;
    }

    public async Task<IReadOnlyList<OrderSkuQuantity>> GetSkuQuantitiesAsync(long orderId, CancellationToken cancellationToken = default)
    {
        return await _orderRepository.GetOrderSkuQuantitiesAsync(orderId, cancellationToken);
    }

    public async Task CancelAsync(long userId, long orderId, string? reason, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
        if (order == null)
            throw new BusinessException("ORDER_NOT_FOUND", "订单不存在");

        if (order.Status != 0)
            throw new BusinessException("ORDER_CANNOT_CANCEL", $"当前订单状态（{order.Status}）不允许取消");

        if (order.UserId != userId)
            throw new BusinessException("FORBIDDEN", "无权取消此订单");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _orderRepository.UpdateOrderStatusAsync(orderId, 4, DateTime.Now, cancellationToken);

            await _orderRepository.InsertOrderLogAsync(new OrderLog
            {
                OrderId = orderId,
                FromStatus = 0,
                ToStatus = 4,
                OperatorId = null,
                Remark = reason ?? "用户主动取消",
                CreatedAt = DateTime.Now
            }, cancellationToken);

            var skuQuantities = await _orderRepository.GetOrderSkuQuantitiesAsync(orderId, cancellationToken);
            await _inventoryService.ReleaseForCancelledOrderAsync(orderId, skuQuantities, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ConfirmAsync(long userId, long orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
        if (order == null)
            throw new BusinessException("ORDER_NOT_FOUND", "订单不存在");

        if (order.Status != 2)
            throw new BusinessException("ORDER_CANNOT_CONFIRM", $"当前订单状态（{order.Status}）不允许确认收货");

        if (order.UserId != userId)
            throw new BusinessException("FORBIDDEN", "无权操作此订单");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _orderRepository.UpdateOrderStatusAsync(orderId, 3, DateTime.Now, cancellationToken);

            await _orderRepository.InsertOrderLogAsync(new OrderLog
            {
                OrderId = orderId,
                FromStatus = 2,
                ToStatus = 3,
                OperatorId = null,
                Remark = "用户确认收货",
                CreatedAt = DateTime.Now
            }, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task MarkPaidAsync(long orderId, long paymentId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
        if (order == null)
            throw new BusinessException("ORDER_NOT_FOUND", "订单不存在");

        if (order.Status != 0)
            throw new BusinessException("ORDER_STATUS_INVALID", $"当前订单状态（{order.Status}）不允许支付");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _orderRepository.UpdateOrderStatusAsync(orderId, 1, DateTime.Now, cancellationToken);

            await _orderRepository.InsertOrderLogAsync(new OrderLog
            {
                OrderId = orderId,
                FromStatus = 0,
                ToStatus = 1,
                OperatorId = null,
                Remark = $"支付成功，支付记录ID：{paymentId}",
                CreatedAt = DateTime.Now
            }, cancellationToken);

            var skuQuantities = await _orderRepository.GetOrderSkuQuantitiesAsync(orderId, cancellationToken);
            await _inventoryService.DeductForPaidOrderAsync(orderId, skuQuantities, cancellationToken);

            if (order.UserCouponId.HasValue)
            {
                await _couponService.UseForOrderAsync(order.UserId, order.UserCouponId.Value, orderId, order.PayAmount, cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task MarkShippedAsync(long orderId, long logisticsId, long operatorId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
        if (order == null)
            throw new BusinessException("ORDER_NOT_FOUND", "订单不存在");

        if (order.Status != 1)
            throw new BusinessException("ORDER_STATUS_INVALID", $"当前订单状态（{order.Status}）不允许发货");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _orderRepository.UpdateOrderStatusAsync(orderId, 2, DateTime.Now, cancellationToken);

            await _orderRepository.InsertOrderLogAsync(new OrderLog
            {
                OrderId = orderId,
                FromStatus = 1,
                ToStatus = 2,
                OperatorId = operatorId,
                Remark = $"已发货，物流ID：{logisticsId}",
                CreatedAt = DateTime.Now
            }, cancellationToken);

            await _operationLogService.WriteAsync(new OperationLogRequest(
                operatorId,
                "SYSTEM",
                "订单管理",
                "发货",
                $"订单 {order.OrderNo} 已发货，物流ID：{logisticsId}",
                "",
                null,
                1
            ), cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // ---------- 私有辅助方法 ----------
    private async Task<AddressDto> ValidateAddressAsync(long userId, long addressId, CancellationToken cancellationToken)
    {
        var addresses = await _addressService.GetMyAddressesAsync(userId, cancellationToken);
        var address = addresses.FirstOrDefault(x => x.AddressId == addressId);
        if (address == null)
            throw new BusinessException("ADDRESS_NOT_FOUND", "地址不存在或不属于您");
        return address;
    }

    private async Task<IReadOnlyList<CartItemWithDetails>> GetSelectedCartItemsAsync(
        long userId,
        IReadOnlyList<long>? cartItemIds,
        CancellationToken cancellationToken)
    {
        var allItems = await _cartRepository.GetUserCartWithDetailsAsync(userId, cancellationToken);

        if (cartItemIds != null && cartItemIds.Count > 0)
        {
            var selected = allItems.Where(x => cartItemIds.Contains(x.CartItemId) && x.Selected).ToList();
            if (selected.Count != cartItemIds.Count)
                throw new BusinessException("CART_ITEM_INVALID", "部分购物车项不存在或未选中");
            return selected;
        }

        var allSelected = allItems.Where(x => x.Selected).ToList();
        if (allSelected.Count == 0)
            throw new BusinessException("CART_EMPTY", "购物车中未选中任何商品");
        return allSelected;
    }

    private static string GenerateOrderNo()
    {
        var now = DateTime.Now;
        var timestamp = now.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999).ToString();
        return $"OD{timestamp}{random}";
    }

    private static OrderDetailDto MapToOrderDetail(OrderMain order)
    {
        var items = order.Items.Select(x => new OrderItemDto(
            x.Id,
            x.SkuId,
            x.ProductNameSnap,
            x.SpecSnap,
            x.MainImageSnap,
            x.UnitPrice,
            x.Quantity,
            x.Subtotal
        )).ToList();

        var logs = order.Logs.Select(x => new OrderLogDto(
            x.Id,
            x.OrderId,
            x.FromStatus,
            x.ToStatus,
            x.OperatorId,
            x.Remark,
            x.CreatedAt
        )).ToList();

        return new OrderDetailDto(
            order.Id,
            order.OrderNo,
            order.UserId,
            order.AddressId,
            order.UserCouponId,
            order.Status,
            order.TotalAmount,
            order.DiscountAmount,
            order.PayAmount,
            order.PayExpireTime,
            order.ReceiverSnapshot,
            order.Remark,
            order.CreatedAt,
            items,
            logs
        );
    }
}
