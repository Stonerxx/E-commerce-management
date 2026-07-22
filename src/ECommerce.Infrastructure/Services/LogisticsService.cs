using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Exceptions;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Infrastructure.Services;

public sealed class LogisticsService : ILogisticsService
{
    private readonly ILogisticsRepository _logisticsRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderService _orderService;
    private readonly IOperationLogService _operationLogService;
    private readonly IUnitOfWork _unitOfWork;

    public LogisticsService(
        ILogisticsRepository logisticsRepository,
        IOrderRepository orderRepository,
        IOrderService orderService,
        IOperationLogService operationLogService,
        IUnitOfWork unitOfWork)
    {
        _logisticsRepository = logisticsRepository;
        _orderRepository = orderRepository;
        _orderService = orderService;
        _operationLogService = operationLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task ShipAsync(
        long orderId,
        ShipmentRequest request,
        long operatorId,
        string operatorName,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateShipment(orderId, request);

        var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken)
            ?? throw new BusinessException("ORDER_NOT_FOUND", "订单不存在");
        if (order.Status != (int)OrderStatus.Paid)
        {
            throw new BusinessException("ORDER_STATUS_INVALID", $"当前订单状态（{order.Status}）不允许发货");
        }

        var shippedAt = request.ShippedAt ?? DateTime.Now;
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var logistics = new Logistics
            {
                OrderId = orderId,
                CompanyName = request.CompanyName.Trim(),
                TrackingNo = request.TrackingNo.Trim(),
                ShippedAt = shippedAt,
                Status = (int)LogisticsStatus.Collected
            };
            var logisticsId = await _logisticsRepository.InsertAsync(logistics, cancellationToken);
            await _logisticsRepository.InsertTrackAsync(new LogisticsTrack
            {
                LogisticsId = logisticsId,
                TrackDesc = "商家已发货，快件已揽收",
                TrackTime = shippedAt,
                Location = null
            }, cancellationToken);

            await _orderService.MarkShippedAsync(
                orderId,
                logisticsId,
                operatorId,
                operatorName,
                ipAddress,
                cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            if (exception is OracleException { Number: 1 })
            {
                throw new BusinessException("LOGISTICS_ALREADY_EXISTS", "该订单已有物流信息或运单号已存在");
            }

            throw;
        }
    }

    public async Task AddTrackAsync(
        long logisticsId,
        LogisticsTrackRequest request,
        long operatorId,
        string operatorName,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateTrack(logisticsId, request);
        var logistics = await _logisticsRepository.GetByIdAsync(logisticsId, cancellationToken)
            ?? throw new BusinessException("LOGISTICS_NOT_FOUND", "物流信息不存在");
        if (request.Status < logistics.Status)
        {
            throw new BusinessException("LOGISTICS_STATUS_INVALID", "物流状态不能回退");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _logisticsRepository.InsertTrackAsync(new LogisticsTrack
            {
                LogisticsId = logisticsId,
                TrackDesc = request.TrackDesc.Trim(),
                TrackTime = request.TrackTime,
                Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim()
            }, cancellationToken);

            var statusChanged = await _logisticsRepository.TryUpdateStatusAsync(
                logisticsId,
                logistics.Status,
                request.Status,
                cancellationToken);
            if (!statusChanged)
            {
                throw new BusinessException("LOGISTICS_STATUS_CHANGED", "物流状态已变化，请刷新后重试");
            }

            await _operationLogService.WriteAsync(new OperationLogRequest(
                operatorId,
                operatorName,
                "物流管理",
                "更新轨迹",
                $"物流 {logisticsId} 更新为状态 {request.Status}：{request.TrackDesc.Trim()}",
                ipAddress,
                null,
                (int)OperationResult.Success), cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<LogisticsDto?> GetByOrderAsync(
        long userId,
        long orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken)
            ?? throw new BusinessException("ORDER_NOT_FOUND", "订单不存在");
        if (order.UserId != userId)
        {
            throw new BusinessException("FORBIDDEN", "无权查看其他用户的订单物流");
        }

        var logistics = await _logisticsRepository.GetByOrderIdAsync(orderId, cancellationToken);
        return logistics is null ? null : MapDto(logistics);
    }

    public async Task<LogisticsDto?> GetByOrderAdminAsync(
        long orderId,
        CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
        {
            throw new BusinessException("VALIDATION_ERROR", "订单 ID 无效");
        }

        _ = await _orderRepository.GetOrderByIdAsync(orderId, cancellationToken)
            ?? throw new BusinessException("ORDER_NOT_FOUND", "订单不存在");
        var logistics = await _logisticsRepository.GetByOrderIdAsync(orderId, cancellationToken);
        return logistics is null ? null : MapDto(logistics);
    }

    private static void ValidateShipment(long orderId, ShipmentRequest request)
    {
        if (orderId <= 0)
        {
            throw new BusinessException("VALIDATION_ERROR", "订单 ID 无效");
        }

        if (string.IsNullOrWhiteSpace(request.CompanyName) || request.CompanyName.Trim().Length > 100)
        {
            throw new BusinessException("VALIDATION_ERROR", "物流公司不能为空且不能超过 100 个字符");
        }

        if (string.IsNullOrWhiteSpace(request.TrackingNo) || request.TrackingNo.Trim().Length > 100)
        {
            throw new BusinessException("VALIDATION_ERROR", "运单号不能为空且不能超过 100 个字符");
        }

        if (request.ShippedAt > DateTime.Now.AddMinutes(5))
        {
            throw new BusinessException("VALIDATION_ERROR", "发货时间不能晚于当前时间");
        }
    }

    private static void ValidateTrack(long logisticsId, LogisticsTrackRequest request)
    {
        if (logisticsId <= 0)
        {
            throw new BusinessException("VALIDATION_ERROR", "物流 ID 无效");
        }

        if (string.IsNullOrWhiteSpace(request.TrackDesc) || request.TrackDesc.Trim().Length > 500)
        {
            throw new BusinessException("VALIDATION_ERROR", "轨迹描述不能为空且不能超过 500 个字符");
        }

        if (!string.IsNullOrWhiteSpace(request.Location) && request.Location.Trim().Length > 200)
        {
            throw new BusinessException("VALIDATION_ERROR", "轨迹位置不能超过 200 个字符");
        }

        if (request.TrackTime > DateTime.Now.AddMinutes(5))
        {
            throw new BusinessException("VALIDATION_ERROR", "轨迹时间不能晚于当前时间");
        }

        if (!Enum.IsDefined(typeof(LogisticsStatus), request.Status))
        {
            throw new BusinessException("VALIDATION_ERROR", "物流状态无效");
        }
    }

    private static LogisticsDto MapDto(Logistics logistics)
    {
        return new LogisticsDto(
            logistics.Id,
            logistics.OrderId,
            logistics.CompanyName,
            logistics.TrackingNo,
            logistics.ShippedAt,
            logistics.Status,
            logistics.Tracks.Select(track => new LogisticsTrackDto(
                track.Id,
                track.TrackDesc,
                track.TrackTime,
                track.Location)).ToList());
    }
}
