using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services;

public sealed class OrderStatisticsSnapshotHostedServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldRefreshConfiguredBackfillBeforeWaitingForTimer()
    {
        var refreshed = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var snapshotService = new Mock<IStatisticsSnapshotService>();
        snapshotService
            .Setup(x => x.RefreshRecentDaysAsync(30, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                refreshed.TrySetResult(30);
                return Task.CompletedTask;
            });

        var provider = new Mock<IServiceProvider>();
        provider.Setup(x => x.GetService(typeof(IStatisticsSnapshotService))).Returns(snapshotService.Object);
        var scope = new Mock<IServiceScope>();
        scope.SetupGet(x => x.ServiceProvider).Returns(provider.Object);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new OrderStatisticsSnapshotHostedService(
            scopeFactory.Object,
            Options.Create(new StatisticsSnapshotOptions
            {
                Enabled = true,
                InitialBackfillDays = 30,
                RefreshIntervalMinutes = 5
            }),
            new Mock<ILogger<OrderStatisticsSnapshotHostedService>>().Object);

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        Assert.Equal(30, await refreshed.Task.WaitAsync(TimeSpan.FromSeconds(2)));

        cts.Cancel();
        await service.StopAsync(CancellationToken.None);
        snapshotService.Verify(x => x.RefreshRecentDaysAsync(30, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenDisabled_ShouldNotResolveSnapshotService()
    {
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var service = new OrderStatisticsSnapshotHostedService(
            scopeFactory.Object,
            Options.Create(new StatisticsSnapshotOptions { Enabled = false }),
            new Mock<ILogger<OrderStatisticsSnapshotHostedService>>().Object);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        scopeFactory.Verify(x => x.CreateScope(), Times.Never);
    }
}
