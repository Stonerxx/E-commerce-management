using System.Data.Common;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Infrastructure.Data;

public sealed class OracleConnectionFactory : IOracleConnectionFactory
{
    private readonly OracleOptions _options;

    public OracleConnectionFactory(IOptions<OracleOptions> options)
    {
        _options = options.Value;
    }

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = BuildConnectionString();
        var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private string BuildConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(_options.ConnectionString)
            && !_options.ConnectionString.Contains("change_me", StringComparison.OrdinalIgnoreCase))
        {
            return _options.ConnectionString;
        }

        var user = Environment.GetEnvironmentVariable("ORACLE_DEV_USER");
        var password = Environment.GetEnvironmentVariable("ORACLE_DEV_PWD");
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Oracle credentials are missing. Please set ORACLE_DEV_USER and ORACLE_DEV_PWD, or set Oracle__ConnectionString.");
        }

        var dataSource = $"{_options.Host}:{_options.Port}/{_options.ServiceName}";
        return $"User Id={user};Password={password};Data Source={dataSource}";
    }
}
