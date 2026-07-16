using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public class LogisticsService : ILogisticsService
{
    private readonly ILogisticsRepository _logisticsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LogisticsService(ILogisticsRepository logisticsRepository, IUnitOfWork unitOfWork)
    {
        _logisticsRepository = logisticsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ShipAsync(long orderId, ShipmentRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var existingLogistics = await _logisticsRepository.GetLogisticsByOrderIdAsync(orderId, cancellationToken);
            if (existingLogistics != null)
            {
                throw new BusinessException("ALREADY_SHIPPED", $"Order {orderId} has already been shipped.");
            }

            var shippedAt = request.ShippedAt ?? DateTime.Now;

            var logistics = new Logistics
            {
                OrderId = orderId,
                CompanyName = request.CompanyName,
                TrackingNo = request.TrackingNo,
                ShippedAt = shippedAt,
                Status = 0 // 0 = 已揽件
            };

            long logisticsId = await _logisticsRepository.InsertLogisticsAsync(logistics, cancellationToken);

            var initialTrack = new LogisticsTrack
            {
                LogisticsId = logisticsId,
                TrackDesc = "包裹已揽件/发货",
                TrackTime = shippedAt,
                Location = "发货仓"
            };

            await _logisticsRepository.InsertTrackAsync(initialTrack, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task AddTrackAsync(long logisticsId, LogisticsTrackRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        var logistics = await _logisticsRepository.GetLogisticsByIdAsync(logisticsId, cancellationToken);
        if (logistics == null)
        {
            throw new BusinessException("NOT_FOUND", $"Logistics {logisticsId} not found.");
        }

        var track = new LogisticsTrack
        {
            LogisticsId = logisticsId,
            TrackDesc = request.TrackDesc,
            TrackTime = request.TrackTime,
            Location = request.Location
        };

        await _logisticsRepository.InsertTrackAsync(track, cancellationToken);
    }

    public async Task<LogisticsDto?> GetByOrderAsync(long userId, long orderId, CancellationToken cancellationToken = default)
    {
        var logistics = await _logisticsRepository.GetLogisticsByOrderIdAsync(orderId, cancellationToken);
        if (logistics == null)
        {
            return null;
        }

        var tracks = await _logisticsRepository.GetTracksByLogisticsIdAsync(logistics.Id, cancellationToken);

        var trackDtos = tracks.Select(t => new LogisticsTrackDto(
            t.Id,
            t.TrackDesc,
            t.TrackTime,
            t.Location
        )).ToList();

        return new LogisticsDto(
            logistics.Id,
            logistics.OrderId,
            logistics.CompanyName,
            logistics.TrackingNo,
            logistics.ShippedAt,
            logistics.Status,
            trackDtos
        );
    }
}
