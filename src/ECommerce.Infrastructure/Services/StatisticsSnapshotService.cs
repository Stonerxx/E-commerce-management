using System.Data.Common;
using System.Data;
using ECommerce.Infrastructure.Data;
using ECommerce.Shared.Abstractions;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Infrastructure.Services;

public sealed class StatisticsSnapshotService : IStatisticsSnapshotService
{
    private readonly IUnitOfWork _unitOfWork;

    public StatisticsSnapshotService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task RefreshRecentDaysAsync(int days, CancellationToken cancellationToken = default)
    {
        var refreshDays = Math.Max(1, days);
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var databaseToday = await GetDatabaseTodayAsync(connection, cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            for (var offset = refreshDays - 1; offset >= 0; offset--)
            {
                await UpsertSnapshotAsync(connection, databaseToday.AddDays(-offset), cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<DateTime> GetDatabaseTodayAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT TRUNC(SYSDATE) FROM DUAL";
        command.Transaction = _unitOfWork.CurrentTransaction;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToDateTime(result).Date;
    }

    private async Task UpsertSnapshotAsync(DbConnection connection, DateTime statDate, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SP_REFRESH_ORDER_STAT_SNAPSHOT";
        command.CommandType = CommandType.StoredProcedure;
        command.Transaction = _unitOfWork.CurrentTransaction;
        if (command is OracleCommand oracleCommand)
        {
            oracleCommand.BindByName = true;
        }

        AddParameter(command, "p_stat_date", statDate.Date);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
