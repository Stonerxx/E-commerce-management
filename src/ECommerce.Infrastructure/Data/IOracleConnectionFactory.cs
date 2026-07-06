using System.Data.Common;

namespace ECommerce.Infrastructure.Data;

public interface IOracleConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
