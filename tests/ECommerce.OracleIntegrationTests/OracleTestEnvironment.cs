using System.Data;
using System.Data.Common;
using ECommerce.Infrastructure.Data;
using Oracle.ManagedDataAccess.Client;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ECommerce.OracleIntegrationTests;

public sealed class DevOracleFactAttribute : FactAttribute
{
    public DevOracleFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(OracleTestEnvironment.DevConnectionString))
        {
            Skip = "Set ECOMMERCE_ORACLE_DEV_CONNECTION_STRING to run write-capable Oracle integration tests.";
        }
    }
}

public sealed class DemoOracleFactAttribute : FactAttribute
{
    public DemoOracleFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(OracleTestEnvironment.DemoConnectionString))
        {
            Skip = "Set ECOMMERCE_ORACLE_DEMO_CONNECTION_STRING to run read-only DEMO validation.";
        }
    }
}

public sealed class LongRunningDevOracleFactAttribute : FactAttribute
{
    public LongRunningDevOracleFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(OracleTestEnvironment.DevConnectionString))
        {
            Skip = "Set ECOMMERCE_ORACLE_DEV_CONNECTION_STRING to run Oracle integration tests.";
        }
        else if (!string.Equals(Environment.GetEnvironmentVariable("ECOMMERCE_ORACLE_LONG_RUNNING"), "1", StringComparison.Ordinal))
        {
            Skip = "Set ECOMMERCE_ORACLE_LONG_RUNNING=1 to run the 5,000-row Excel export test.";
        }
    }
}

public static class OracleTestEnvironment
{
    public const string DevConnectionStringVariable = "ECOMMERCE_ORACLE_DEV_CONNECTION_STRING";
    public const string DemoConnectionStringVariable = "ECOMMERCE_ORACLE_DEMO_CONNECTION_STRING";

    public static string? DevConnectionString => Environment.GetEnvironmentVariable(DevConnectionStringVariable);
    public static string? DemoConnectionString => Environment.GetEnvironmentVariable(DemoConnectionStringVariable);

    public static async Task<OracleConnection> OpenDevAsync(CancellationToken cancellationToken = default)
    {
        return await OpenAsync(DevConnectionString!, cancellationToken);
    }

    public static async Task<OracleConnection> OpenDemoAsync(CancellationToken cancellationToken = default)
    {
        return await OpenAsync(DemoConnectionString!, cancellationToken);
    }

    public static async Task<(long UserId, long AddressId, long ProductId)> GetSeedReferencesAsync(
        OracleConnection connection,
        CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection, @"
            SELECT
                (SELECT user_id FROM ADDRESS WHERE is_deleted = 0 ORDER BY id FETCH FIRST 1 ROW ONLY) AS user_id,
                (SELECT id FROM ADDRESS WHERE is_deleted = 0 ORDER BY id FETCH FIRST 1 ROW ONLY) AS address_id,
                (SELECT id FROM PRODUCT ORDER BY id FETCH FIRST 1 ROW ONLY) AS product_id
            FROM DUAL");

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        Assert.True(await reader.ReadAsync(cancellationToken), "Expected a row from DUAL.");
        Assert.False(reader.IsDBNull(0), "DEV database has no active ADDRESS rows. Run init_database.sql and seed_demo_data.sql manually first.");
        Assert.False(reader.IsDBNull(1), "DEV database has no ADDRESS rows. Run seed_demo_data.sql manually first.");
        Assert.False(reader.IsDBNull(2), "DEV database has no PRODUCT rows. Run seed_demo_data.sql manually first.");
        return (reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2));
    }

    public static OracleCommand CreateCommand(OracleConnection connection, string sql, OracleTransaction? transaction = null)
    {
        var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = sql;
        command.Transaction = transaction;
        return command;
    }

    public static long NewId()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 10_000 + Random.Shared.Next(1, 10_000);
    }

    private static async Task<OracleConnection> OpenAsync(string connectionString, CancellationToken cancellationToken)
    {
        var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}

public sealed class TestOracleConnectionFactory : IOracleConnectionFactory
{
    private readonly string _connectionString;

    public TestOracleConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
