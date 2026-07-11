namespace ECommerce.Infrastructure.Services;

public interface IStatisticsSnapshotService
{
    Task RefreshRecentDaysAsync(int days, CancellationToken cancellationToken = default);
}
