namespace ECommerce.Infrastructure.Data;

public interface IDatabaseHealthCheck
{
    Task<DatabaseCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
