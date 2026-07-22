using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Models;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<OrderService> _logger;

    private const int PAY_EXPIRE_MINUTES = 30;

    public OrderService(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        ISkuService skuService,
        IUnitOfWork unitOfWork,
        IAddressService addressService,
        IInventoryService inventoryService,
        ICouponService couponService,
        IOperationLogService operationLogService,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _skuService = skuService;
        _unitOfWork = unitOfWork;
        _addressService = addressService;
        _inventoryService = inventoryService;
        _couponService = couponService;
        _operationLogService = operationLogService;
        _logger = logger;
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
            if (sku.Status != (int)SkuStatus.Enabled)
                throw new BusinessException("SKU_NOT_AVAILABLE", $"SKU {item.SkuId} 已停售");
            if (sku.ProductStatus is not ((int)ProductStatus.OnShelf or (int)ProductStatus.Presale))
                throw new BusinessException("PRODUCT_OFF_SHELF", $"商品 {item.SkuId} 已下架");

            var availableStock = sku.Stock - sku.LockedStock;
            if (availableStock < item.Quantity)
                throw new BusinessException("INSUFFICIENT_STOCK", $"SKU {item.SkuId} 库存不足，当前可用：{availableStock}");
        }

        // 4. 计算金额
        decimal totalAmount = cartItems.Sum(x => x.UnitPrice * x.Quantity);
        var discountAmount = await GetDiscountAmountAsync(
            userId,
            request.UserCouponId,
            totalAmount,
            cancellationToken);

        var payAmount = totalAmount - discountAmount;
        if (payAmount < 0) payAmount = 0;

        // 6. 构造预览明细
        var items = cartItems.Select(x => new OrderItemDto(
            0,
            x.SkuId,
            x.ProductId,
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
        string? orderNo = null;

        try
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
                if (sku.Status != (int)SkuStatus.Enabled)
                    throw new BusinessException("SKU_NOT_AVAILABLE", $"SKU {item.SkuId} 已停售");
                if (sku.ProductStatus is not ((int)ProductStatus.OnShelf or (int)ProductStatus.Presale))
                    throw new BusinessException("PRODUCT_OFF_SHELF", $"商品 {item.SkuId} 已下架");

                var availableStock = sku.Stock - sku.LockedStock;
                if (availableStock < item.Quantity)
                    throw new BusinessException("INSUFFICIENT_STOCK", $"SKU {item.SkuId} 库存不足，当前可用：{availableStock}");
            }

            // 3. 计算金额
            decimal totalAmount = cartItems.Sum(x => x.UnitPrice * x.Quantity);
            // 4. 生成订单编号
            orderNo = GenerateOrderNo();

            // 5. 开启事务
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // 金额必须由服务端购物车重新计算，并在订单事务中再次验证优惠券。
            var discountAmount = await GetDiscountAmountAsync(
                userId,
                request.UserCouponId,
                totalAmount,
                cancellationToken);
            var payAmount = Math.Max(0, totalAmount - discountAmount);

            // 6. 创建订单主表
            var order = new OrderMain
            {
                OrderNo = orderNo,
                UserId = userId,
                AddressId = request.AddressId,
                UserCouponId = request.UserCouponId,
                Status = (int)OrderStatus.PendingPayment,  // 使用枚举
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

            if (request.UserCouponId.HasValue)
            {
                await _couponService.UseForOrderAsync(
                    userId,
                    request.UserCouponId.Value,
                    orderId,
                    totalAmount,
                    discountAmount,
                    cancellationToken);
            }

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
                ToStatus = (int)OrderStatus.PendingPayment,
                OperatorId = null,
                Remark = "用户创建订单",
                CreatedAt = DateTime.Now
            }, cancellationToken);

            // 9. 锁定库存（调用库存服务）
            var skuQuantities = items.Select(x => new OrderSkuQuantity(x.SkuId, x.Quantity)).ToList();
            await _inventoryService.LockForOrderAsync(orderId, skuQuantities, cancellationToken);

            // 10. 清空购物车。指定下单项时只能删除本次提交的记录。
            if (request.CartItemIds is { Count: > 0 })
            {
                await _cartRepository.ClearByIdsAsync(
                    userId,
                    cartItems.Select(item => item.CartItemId).ToArray(),
                    cancellationToken);
            }
            else
            {
                await _cartRepository.ClearSelectedAsync(userId, cancellationToken);
            }

            // 11. 提交事务
            await _unitOfWork.CommitAsync(cancellationToken);

            return orderId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建订单失败，UserId: {UserId}, OrderNo: {OrderNo}", userId, orderNo ?? "未生成");
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

    public async Task CancelAsync(long userId, long orderId, long operatorId, string operatorName, string ipAddress, string? reason, CancellationToken cancellationToken = default)
    {
        // 1. 查询订单
        var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
        if (order == null)
            throw new BusinessException("ORDER_NOT_FOUND", "订单不存在");

        if (order.UserId != userId)
            throw new BusinessException("FORBIDDEN", "无权操作此订单");

        // 2. 校验订单状态：只有待支付可以取消
        if (order.Status != (int)OrderStatus.PendingPayment)
            throw new BusinessException("ORDER_CANNOT_CANCEL", $"当前订单状态（{order.Status}）不允许取消");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 3. 更新订单状态为已取消
            var statusChanged = await _orderRepository.TryUpdateStatusAsync(
                orderId,
                (int)OrderStatus.PendingPayment,
                (int)OrderStatus.Cancelled,
                DateTime.Now,
                cancellationToken);
            EnsureStatusChanged(statusChanged);

            // 4. 写入订单状态日志（ORDER_LOG）
            await _orderRepository.InsertOrderLogAsync(new OrderLog
            {
                OrderId = orderId,
                FromStatus = (int)OrderStatus.PendingPayment,
                ToStatus = (int)OrderStatus.Cancelled,
                OperatorId = operatorId,  // 记录实际操作人
                Remark = reason ?? (operatorId == userId ? "用户主动取消" : "后台管理员取消"),
                CreatedAt = DateTime.Now
            }, cancellationToken);

            // 5. 释放锁定库存
            var skuQuantities = await _orderRepository.GetOrderSkuQuantitiesAsync(orderId, cancellationToken);
            await _inventoryService.ReleaseForCancelledOrderAsync(orderId, skuQuantities, cancellationToken);

            if (order.UserCouponId.HasValue)
            {
                await _couponService.RestoreForOrderAsync(
                    order.UserId,
                    order.UserCouponId.Value,
                    orderId,
                    cancellationToken);
            }

            // 6. 判断是否需要写入 OPERATION_LOG
            //    规范要求：只有"后台关键写操作"才写入 OPERATION_LOG
            //    判断标准：操作人不是订单主人 → 视为后台管理员操作 → 写入 OPERATION_LOG
            //              操作人是订单主人 → 前台用户自取消 → 只写 ORDER_LOG，不写 OPERATION_LOG
            if (operatorId != userId)
            {
                // 后台管理员取消订单 → 必须写入 OPERATION_LOG
                await _operationLogService.WriteAsync(new OperationLogRequest(
                    OperatorId: operatorId,
                    OperatorName: operatorName,  // 真实管理员姓名
                    Module: "订单管理",
                    Action: "后台取消订单",
                    Description: $"管理员 {operatorName} 取消订单 {order.OrderNo}，原因：{reason ?? "无"}",
                    IpAddress: ipAddress,
                    RequestParams: null,
                    Result: (int)OperationResult.Success
                ), cancellationToken);
            }
            // 如果 operatorId == userId，说明是用户自己取消，不写入 OPERATION_LOG

            // 8. 提交事务
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "取消订单失败，OrderId: {OrderId}, OperatorId: {OperatorId}, UserId: {UserId}",
                orderId, operatorId, userId);
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ConfirmAsync(long userId, long orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
        if (order == null)
            throw new BusinessException("ORDER_NOT_FOUND", "订单不存在");

        if (order.Status != (int)OrderStatus.Shipped)
            throw new BusinessException("ORDER_CANNOT_CONFIRM", $"当前订单状态（{order.Status}）不允许确认收货");

        if (order.UserId != userId)
            throw new BusinessException("FORBIDDEN", "无权操作此订单");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var statusChanged = await _orderRepository.TryUpdateStatusAsync(
                orderId,
                (int)OrderStatus.Shipped,
                (int)OrderStatus.Completed,
                DateTime.Now,
                cancellationToken);
            EnsureStatusChanged(statusChanged);

            await _orderRepository.InsertOrderLogAsync(new OrderLog
            {
                OrderId = orderId,
                FromStatus = (int)OrderStatus.Shipped,
                ToStatus = (int)OrderStatus.Completed,
                OperatorId = userId,
                Remark = "用户确认收货",
                CreatedAt = DateTime.Now
            }, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "确认收货失败，OrderId: {OrderId}", orderId);
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task MarkPaidAsync(long orderId, long paymentId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
        if (order == null)
            throw new BusinessException("ORDER_NOT_FOUND", "订单不存在");

        if (order.Status is (int)OrderStatus.Paid or (int)OrderStatus.Shipped or (int)OrderStatus.Completed)
        {
            _logger.LogWarning("订单 {OrderId} 已完成支付处理，重复调用 MarkPaidAsync", orderId);
            return;
        }

        if (order.Status != (int)OrderStatus.PendingPayment)
            throw new BusinessException("ORDER_STATUS_INVALID", $"当前订单状态（{order.Status}）不允许支付");

        if (_unitOfWork.CurrentTransaction is not null)
        {
            await MarkPaidCoreAsync(order, paymentId, cancellationToken);
            return;
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await MarkPaidCoreAsync(order, paymentId, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "支付成功处理失败，OrderId: {OrderId}, PaymentId: {PaymentId}", orderId, paymentId);
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task MarkPaidCoreAsync(
        OrderMain order,
        long paymentId,
        CancellationToken cancellationToken)
    {
        var statusChanged = await _orderRepository.TryUpdateStatusAsync(
            order.Id,
            (int)OrderStatus.PendingPayment,
            (int)OrderStatus.Paid,
            DateTime.Now,
            cancellationToken);
        EnsureStatusChanged(statusChanged);

        await _orderRepository.InsertOrderLogAsync(new OrderLog
        {
            OrderId = order.Id,
            FromStatus = (int)OrderStatus.PendingPayment,
            ToStatus = (int)OrderStatus.Paid,
            OperatorId = null,
            Remark = $"支付成功，支付记录ID：{paymentId}",
            CreatedAt = DateTime.Now
        }, cancellationToken);

        var skuQuantities = await _orderRepository.GetOrderSkuQuantitiesAsync(order.Id, cancellationToken);
        await _inventoryService.DeductForPaidOrderAsync(order.Id, skuQuantities, cancellationToken);

        // 商品销量由 TRG_ORDER_PAID_UPDATE_SALES 在订单状态成功更新后维护。
    }

    public async Task MarkShippedAsync(long orderId, long logisticsId, long operatorId, string operatorName, string ipAddress, CancellationToken cancellationToken = default)
    {
        if (_unitOfWork.CurrentTransaction is not null)
        {
            await MarkShippedCoreAsync(orderId, logisticsId, operatorId, operatorName, ipAddress, cancellationToken);
            return;
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await MarkShippedCoreAsync(orderId, logisticsId, operatorId, operatorName, ipAddress, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发货失败，OrderId: {OrderId}", orderId);
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task MarkShippedCoreAsync(
        long orderId,
        long logisticsId,
        long operatorId,
        string operatorName,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
        if (order == null)
            throw new BusinessException("ORDER_NOT_FOUND", "订单不存在");

        if (order.Status != (int)OrderStatus.Paid)
            throw new BusinessException("ORDER_STATUS_INVALID", $"当前订单状态（{order.Status}）不允许发货");

        var statusChanged = await _orderRepository.TryUpdateStatusAsync(
            orderId,
            (int)OrderStatus.Paid,
            (int)OrderStatus.Shipped,
            DateTime.Now,
            cancellationToken);
        EnsureStatusChanged(statusChanged);

        await _orderRepository.InsertOrderLogAsync(new OrderLog
        {
            OrderId = orderId,
            FromStatus = (int)OrderStatus.Paid,
            ToStatus = (int)OrderStatus.Shipped,
            OperatorId = operatorId,
            OperatorName = operatorName,
            Remark = $"已发货，物流ID：{logisticsId}",
            CreatedAt = DateTime.Now
        }, cancellationToken);

        await _operationLogService.WriteAsync(new OperationLogRequest(
            operatorId,
            operatorName,
            "订单管理",
            "发货",
            $"订单 {order.OrderNo} 已发货，物流ID：{logisticsId}",
            ipAddress,
            null,
            (int)OperationResult.Success
        ), cancellationToken);
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

    private async Task<decimal> GetDiscountAmountAsync(
        long userId,
        long? userCouponId,
        decimal orderAmount,
        CancellationToken cancellationToken)
    {
        if (!userCouponId.HasValue)
        {
            return 0;
        }

        var validation = await _couponService.ValidateAsync(
            userId,
            userCouponId.Value,
            orderAmount,
            cancellationToken);
        if (!validation.Available)
        {
            throw new BusinessException("COUPON_NOT_AVAILABLE", validation.Reason ?? "优惠券不可用");
        }

        return validation.DiscountAmount;
    }

    private static void EnsureStatusChanged(bool statusChanged)
    {
        if (!statusChanged)
        {
            throw new BusinessException("ORDER_STATUS_CHANGED", "订单状态已变化，请刷新后重试");
        }
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
            x.ProductId,
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
            x.OperatorName,
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
