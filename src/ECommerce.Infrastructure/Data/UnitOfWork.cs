using System.Data;
using System.Data.Common;
using ECommerce.Shared.Abstractions;

namespace ECommerce.Infrastructure.Data;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private DbConnection? _connection;
    private DbTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(IOracleConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public DbConnection? CurrentConnection => _connection;

    public DbTransaction? CurrentTransaction => _transaction;

    public async Task<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_connection is null)
        {
            _connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            return _connection;
        }

        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }

        return _connection;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_transaction is not null)
        {
            throw new InvalidOperationException("A database transaction has already been started in this unit of work.");
        }

        var connection = await GetOpenConnectionAsync(cancellationToken);
        _transaction = await connection.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_transaction is null)
        {
            throw new InvalidOperationException("No database transaction has been started.");
        }

        await _transaction.CommitAsync(cancellationToken);
        await DisposeTransactionAsync();
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken);
        await DisposeTransactionAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_transaction is not null)
        {
            try
            {
                await _transaction.RollbackAsync();
            }
            catch
            {
                // Dispose should not hide the original request failure.
            }

            await DisposeTransactionAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.DisposeAsync();
        _transaction = null;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
