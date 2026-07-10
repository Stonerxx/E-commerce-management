using ECommerce.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services;

public sealed class OrderStatisticsSnapshotHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly StatisticsSnapshotOptions _options;
    private readonly ILogger<OrderStatisticsSnapshotHostedService> _logger;

    public OrderStatisticsSnapshotHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<StatisticsSnapshotOptions> options,
        ILogger<OrderStatisticsSnapshotHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Order statistics snapshot refresh is disabled.");
            return;
        }

        await RefreshAsync(Math.Max(1, _options.InitialBackfillDays), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, _options.RefreshIntervalMinutes)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshAsync(2, stoppingToken);
        }
    }

    private async Task RefreshAsync(int days, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var snapshotService = scope.ServiceProvider.GetRequiredService<IStatisticsSnapshotService>();
            await snapshotService.RefreshRecentDaysAsync(days, cancellationToken);
            _logger.LogInformation("Order statistics snapshots refreshed for {Days} day(s).", days);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order statistics snapshot refresh failed. It will retry on the next interval.");
        }
    }
}
