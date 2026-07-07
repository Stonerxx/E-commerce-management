using System.Data.Common;

namespace ECommerce.Shared.Abstractions;

public interface IUnitOfWork : IAsyncDisposable
{
    DbConnection? CurrentConnection { get; }

    DbTransaction? CurrentTransaction { get; }

    Task<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
