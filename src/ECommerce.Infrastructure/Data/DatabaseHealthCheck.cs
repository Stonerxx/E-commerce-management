using System.Globalization;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Data;

public sealed class DatabaseHealthCheck : IDatabaseHealthCheck
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(
        IOracleConnectionFactory connectionFactory,
        ILogger<DatabaseHealthCheck> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<DatabaseCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT SYSDATE FROM DUAL";

            var serverTime = await command.ExecuteScalarAsync(cancellationToken);
            return new DatabaseCheckResult(
                true,
                "Oracle",
                Convert.ToString(serverTime, CultureInfo.InvariantCulture),
                null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Oracle database health check failed.");
            return new DatabaseCheckResult(false, "Oracle", null, ex.Message);
        }
    }
}
