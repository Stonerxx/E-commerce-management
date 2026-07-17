using ECommerce.Application.DTOs;
using ECommerce.Infrastructure.Services;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Services;

public sealed class StatisticsServiceTests
{
    [Fact]
    public async Task GetOrderStatisticsAsync_RejectsUnsupportedDimensionBeforeOpeningConnection()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var service = new StatisticsService(unitOfWork.Object, Mock.Of<ILogger<StatisticsService>>());

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.GetOrderStatisticsAsync(
            new StatisticsQuery(DateTime.Today.AddDays(-30), DateTime.Today, "week")));

        Assert.Equal("STATISTICS_DIMENSION_INVALID", exception.Code);
        unitOfWork.Verify(
            item => item.GetOpenConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetOrderStatisticsAsync_RejectsReversedDateRange()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var service = new StatisticsService(unitOfWork.Object, Mock.Of<ILogger<StatisticsService>>());

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.GetOrderStatisticsAsync(
            new StatisticsQuery(DateTime.Today, DateTime.Today.AddDays(-1), "month")));

        Assert.Equal("STATISTICS_RANGE_INVALID", exception.Code);
        unitOfWork.Verify(
            item => item.GetOpenConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
