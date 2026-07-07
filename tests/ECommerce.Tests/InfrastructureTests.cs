using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Tests;

public sealed class InfrastructureTests
{
    [Fact]
    public async Task UnitOfWork_ShouldOpenConnectionAndCommitTransaction()
    {
        var factory = new FakeOracleConnectionFactory();
        await using var unitOfWork = new UnitOfWork(factory);

        var connection = await unitOfWork.GetOpenConnectionAsync();
        await unitOfWork.BeginTransactionAsync();

        Assert.Same(connection, unitOfWork.CurrentConnection);
        Assert.NotNull(unitOfWork.CurrentTransaction);
        Assert.Equal(1, factory.CreatedConnections);

        await unitOfWork.CommitAsync();

        Assert.Null(unitOfWork.CurrentTransaction);
        Assert.Same(connection, unitOfWork.CurrentConnection);
    }

    [Fact]
    public async Task UnitOfWork_ShouldRejectCommitWithoutTransaction()
    {
        var factory = new FakeOracleConnectionFactory();
        await using var unitOfWork = new UnitOfWork(factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.CommitAsync());
    }

    private sealed class FakeOracleConnectionFactory : IOracleConnectionFactory
    {
        public int CreatedConnections { get; private set; }

        public Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            CreatedConnections++;
            return Task.FromResult<DbConnection>(new FakeDbConnection());
        }
    }

    private sealed class FakeDbConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Open;

        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "FakeDatabase";

        public override string DataSource => "FakeDataSource";

        public override string ServerVersion => "1.0";

        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName)
        {
        }

        public override void Close()
        {
            _state = ConnectionState.Closed;
        }

        public override void Open()
        {
            _state = ConnectionState.Open;
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            _state = ConnectionState.Open;
            return Task.CompletedTask;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new FakeDbTransaction(this, isolationLevel);
        }

        protected override DbCommand CreateDbCommand()
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeDbTransaction : DbTransaction
    {
        private readonly DbConnection _connection;

        public FakeDbTransaction(DbConnection connection, IsolationLevel isolationLevel)
        {
            _connection = connection;
            IsolationLevel = isolationLevel;
        }

        public override IsolationLevel IsolationLevel { get; }

        protected override DbConnection DbConnection => _connection;

        public override void Commit()
        {
        }

        public override void Rollback()
        {
        }
    }
}
