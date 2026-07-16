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
        if (!_options.HasUsableConnectionString)
        {
            throw new InvalidOperationException(
                "Oracle connection string is not configured. Set Oracle__ConnectionString before checking the database.");
        }

        var connection = new OracleConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
