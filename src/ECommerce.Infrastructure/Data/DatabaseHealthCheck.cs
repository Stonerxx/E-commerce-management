using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Data;

public sealed class DatabaseHealthCheck : IDatabaseHealthCheck
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseHealthCheck> _logger;
    private readonly OracleOptions _options;

    public DatabaseHealthCheck(
        IOracleConnectionFactory connectionFactory,
        ILogger<DatabaseHealthCheck> logger,
        IOptions<OracleOptions> options)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<DatabaseCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var checkedAt = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        if (!_options.HasUsableConnectionString)
        {
            stopwatch.Stop();
            return new DatabaseCheckResult(
                false,
                false,
                "Oracle",
                null,
                stopwatch.ElapsedMilliseconds,
                checkedAt,
                "Oracle connection string is not configured. Set Oracle__ConnectionString.");
        }

        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT SYSTIMESTAMP FROM DUAL";
            command.CommandTimeout = Math.Max(1, _options.HealthCheckTimeoutSeconds);

            var serverTime = await command.ExecuteScalarAsync(cancellationToken);
            stopwatch.Stop();
            return new DatabaseCheckResult(
                true,
                true,
                "Oracle",
                Convert.ToString(serverTime, CultureInfo.InvariantCulture),
                stopwatch.ElapsedMilliseconds,
                checkedAt,
                null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Oracle database health check failed.");
            return new DatabaseCheckResult(
                false,
                true,
                "Oracle",
                null,
                stopwatch.ElapsedMilliseconds,
                checkedAt,
                ex.Message);
        }
    }
}
