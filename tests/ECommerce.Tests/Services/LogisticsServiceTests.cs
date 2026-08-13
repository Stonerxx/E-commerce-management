using System.Data.Common;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Exceptions;
using Moq;

namespace ECommerce.Tests.Services;

public sealed class LogisticsServiceTests
{
    private readonly Mock<ILogisticsRepository> _logistics = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IOrderService> _orderService = new();
    private readonly Mock<IOperationLogService> _operationLogs = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly LogisticsService _service;

    public LogisticsServiceTests()
    {
        _unitOfWork.Setup(item => item.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(item => item.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(item => item.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _service = new LogisticsService(
            _logistics.Object,
            _orders.Object,
            _orderService.Object,
            _operationLogs.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task ShipAsync_RejectsOrderThatIsNotPaid()
    {
        _orders.Setup(item => item.GetOrderByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(OrderStatus.PendingPayment));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _service.ShipAsync(
            10,
            CreateShipment(),
            5,
            "admin",
            "127.0.0.1"));

        Assert.Equal("ORDER_STATUS_INVALID", exception.Code);
        _unitOfWork.Verify(item => item.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShipAsync_CreatesLogisticsTrackAndOrderTransitionInOneTransaction()
    {
        _orders.Setup(item => item.GetOrderByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(OrderStatus.Paid));
        _logistics.Setup(item => item.InsertAsync(It.IsAny<Logistics>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(30);

        await _service.ShipAsync(10, CreateShipment(), 5, "admin", "127.0.0.1");

        _unitOfWork.Verify(item => item.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _logistics.Verify(item => item.InsertAsync(
            It.Is<Logistics>(value => value.OrderId == 10 && value.Status == (int)LogisticsStatus.Collected),
            It.IsAny<CancellationToken>()), Times.Once);
        _logistics.Verify(item => item.InsertTrackAsync(
            It.Is<LogisticsTrack>(value => value.LogisticsId == 30 && value.TrackDesc.Contains("已揽收")),
            It.IsAny<CancellationToken>()), Times.Once);
        _orderService.Verify(item => item.MarkShippedAsync(
            10,
            30,
            5,
            "admin",
            "127.0.0.1",
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(item => item.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ShipAsync_RollsBackWhenOrderTransitionFails()
    {
        _orders.Setup(item => item.GetOrderByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(OrderStatus.Paid));
        _logistics.Setup(item => item.InsertAsync(It.IsAny<Logistics>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(30);
        _orderService.Setup(item => item.MarkShippedAsync(
                10,
                30,
                5,
                "admin",
                "127.0.0.1",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessException("ORDER_STATUS_CHANGED", "状态已变化"));

        await Assert.ThrowsAsync<BusinessException>(() => _service.ShipAsync(
            10,
            CreateShipment(),
            5,
            "admin",
            "127.0.0.1"));

        _unitOfWork.Verify(item => item.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(item => item.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByOrderAsync_RejectsAnotherUsersOrder()
    {
        _orders.Setup(item => item.GetOrderByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderMain { Id = 10, UserId = 2 });

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _service.GetByOrderAsync(1, 10));

        Assert.Equal("FORBIDDEN", exception.Code);
        _logistics.Verify(item => item.GetByOrderIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByOrderAsync_ReturnsOwnedOrderTracks()
    {
        _orders.Setup(item => item.GetOrderByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderMain { Id = 10, UserId = 1 });
        _logistics.Setup(item => item.GetByOrderIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Logistics
            {
                Id = 30,
                OrderId = 10,
                CompanyName = "测试物流",
                TrackingNo = "TRACK-1",
                Status = (int)LogisticsStatus.InTransit,
                Tracks = new[]
                {
                    new LogisticsTrack { Id = 40, LogisticsId = 30, TrackDesc = "运输中", TrackTime = DateTime.Now }
                }
            });

        var result = await _service.GetByOrderAsync(1, 10);

        Assert.NotNull(result);
        Assert.Equal(30, result.LogisticsId);
        Assert.Single(result.Tracks);
    }

    [Fact]
    public async Task GetByOrderAdminAsync_ReturnsLogisticsWithoutCustomerOwnershipRule()
    {
        _orders.Setup(item => item.GetOrderByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderMain { Id = 10, UserId = 99 });
        _logistics.Setup(item => item.GetByOrderIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Logistics
            {
                Id = 30,
                OrderId = 10,
                CompanyName = "测试物流",
                TrackingNo = "TRACK-1",
                Status = (int)LogisticsStatus.InTransit
            });

        var result = await _service.GetByOrderAdminAsync(10);

        Assert.NotNull(result);
        Assert.Equal(30, result.LogisticsId);
    }

    [Fact]
    public async Task AddTrackAsync_RejectsStatusRegression()
    {
        _logistics.Setup(item => item.GetByIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLogistics(LogisticsStatus.Delivering));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _service.AddTrackAsync(
            30,
            CreateTrack(LogisticsStatus.InTransit),
            5,
            "service",
            "127.0.0.1"));

        Assert.Equal("LOGISTICS_STATUS_INVALID", exception.Code);
    }

    [Fact]
    public async Task AddTrackAsync_UpdatesTrackStatusAndAuditInOneTransaction()
    {
        _logistics.Setup(item => item.GetByIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLogistics(LogisticsStatus.InTransit));
        _logistics.Setup(item => item.TryUpdateStatusAsync(
                30,
                (int)LogisticsStatus.InTransit,
                (int)LogisticsStatus.Delivering,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.AddTrackAsync(
            30,
            CreateTrack(LogisticsStatus.Delivering),
            5,
            "service",
            "127.0.0.1");

        _logistics.Verify(item => item.InsertTrackAsync(
            It.Is<LogisticsTrack>(value => value.LogisticsId == 30),
            It.IsAny<CancellationToken>()), Times.Once);
        _operationLogs.Verify(item => item.WriteAsync(
            It.Is<OperationLogRequest>(value => value.OperatorId == 5 && value.Module == "物流管理"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(item => item.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ShipmentRequest CreateShipment()
    {
        return new ShipmentRequest("测试物流", "TRACK-1", DateTime.Now);
    }

    private static LogisticsTrackRequest CreateTrack(LogisticsStatus status)
    {
        return new LogisticsTrackRequest("更新轨迹", DateTime.Now, "深圳", (int)status);
    }

    private static OrderMain CreateOrder(OrderStatus status)
    {
        return new OrderMain { Id = 10, UserId = 1, Status = (int)status };
    }

    private static Logistics CreateLogistics(LogisticsStatus status)
    {
        return new Logistics { Id = 30, OrderId = 10, Status = (int)status };
    }
}
