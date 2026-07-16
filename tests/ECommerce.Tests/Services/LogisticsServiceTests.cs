using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Exceptions;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services;

public class LogisticsServiceTests
{
    private readonly Mock<ILogisticsRepository> _mockRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly LogisticsService _sut;

    public LogisticsServiceTests()
    {
        _mockRepo = new Mock<ILogisticsRepository>();
        _mockUow = new Mock<IUnitOfWork>();
        _sut = new LogisticsService(_mockRepo.Object, _mockUow.Object);
    }

    [Fact]
    public async Task ShipAsync_ShouldThrowIfAlreadyShipped()
    {
        _mockRepo.Setup(x => x.GetLogisticsByOrderIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Logistics { Id = 1 });

        var request = new ShipmentRequest("SF", "123", DateTime.Now);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.ShipAsync(1, request, 1));
        Assert.Equal("ALREADY_SHIPPED", ex.Code);
    }

    [Fact]
    public async Task ShipAsync_ShouldInsertLogisticsAndInitialTrack()
    {
        _mockRepo.Setup(x => x.GetLogisticsByOrderIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Logistics?)null);
                 
        _mockRepo.Setup(x => x.InsertLogisticsAsync(It.IsAny<Logistics>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(99);

        var request = new ShipmentRequest("SF", "123", DateTime.Now);

        await _sut.ShipAsync(1, request, 1);

        _mockUow.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(x => x.InsertLogisticsAsync(It.Is<Logistics>(l => l.OrderId == 1 && l.CompanyName == "SF"), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(x => x.InsertTrackAsync(It.Is<LogisticsTrack>(t => t.LogisticsId == 99 && t.TrackDesc.Contains("揽件")), It.IsAny<CancellationToken>()), Times.Once);
        _mockUow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTrackAsync_ShouldThrowIfLogisticsNotFound()
    {
        _mockRepo.Setup(x => x.GetLogisticsByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Logistics?)null);

        var request = new LogisticsTrackRequest("Arrived", DateTime.Now, "Beijing");

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.AddTrackAsync(99, request, 1));
        Assert.Equal("NOT_FOUND", ex.Code);
    }

    [Fact]
    public async Task AddTrackAsync_ShouldInsertTrack()
    {
        _mockRepo.Setup(x => x.GetLogisticsByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Logistics { Id = 99 });

        var request = new LogisticsTrackRequest("Arrived", DateTime.Now, "Beijing");

        await _sut.AddTrackAsync(99, request, 1);

        _mockRepo.Verify(x => x.InsertTrackAsync(It.Is<LogisticsTrack>(t => t.LogisticsId == 99 && t.Location == "Beijing"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByOrderAsync_ShouldReturnMappedDtoWithTracks()
    {
        _mockRepo.Setup(x => x.GetLogisticsByOrderIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Logistics { Id = 99, CompanyName = "SF", TrackingNo = "123" });
                 
        _mockRepo.Setup(x => x.GetTracksByLogisticsIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<LogisticsTrack> 
                 { 
                     new LogisticsTrack { Id = 1, TrackDesc = "A" },
                     new LogisticsTrack { Id = 2, TrackDesc = "B" }
                 });

        var result = await _sut.GetByOrderAsync(1, 1);

        Assert.NotNull(result);
        Assert.Equal(99, result.LogisticsId);
        Assert.Equal("SF", result.CompanyName);
        Assert.Equal(2, result.Tracks.Count);
        Assert.Equal("A", result.Tracks[0].TrackDesc);
    }
}
