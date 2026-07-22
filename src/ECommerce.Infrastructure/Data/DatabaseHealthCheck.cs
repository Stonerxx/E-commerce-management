using System.Diagnostics;
using System.Globalization;
using System.Data.Common;
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
                null,
                null,
                null,
                stopwatch.ElapsedMilliseconds,
                checkedAt,
                "Oracle connection string is not configured. Set Oracle__ConnectionString.");
        }

        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    SYS_CONTEXT('USERENV', 'SESSION_USER') AS session_user,
                    SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') AS current_schema,
                    SYS_CONTEXT('USERENV', 'SERVICE_NAME') AS service_name,
                    SYSTIMESTAMP AS server_time
                FROM DUAL";
            command.CommandTimeout = Math.Max(1, _options.HealthCheckTimeoutSeconds);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Oracle health check query returned no rows.");
            }

            stopwatch.Stop();
            return new DatabaseCheckResult(
                true,
                true,
                "Oracle",
                ReadNullableString(reader, 0),
                ReadNullableString(reader, 1),
                ReadNullableString(reader, 2),
                Convert.ToString(reader.GetValue(3), CultureInfo.InvariantCulture),
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
                null,
                null,
                null,
                stopwatch.ElapsedMilliseconds,
                checkedAt,
                ex.Message);
        }
    }

    private static string? ReadNullableString(DbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? null
            : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }
}
