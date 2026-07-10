using System.Data.Common;
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
        command.CommandText = """
            MERGE INTO ORDER_STAT_SNAPSHOT target
            USING (
                SELECT
                    :StatDate AS stat_date,
                    (SELECT COUNT(1) FROM ORDER_MAIN om
                     WHERE om.created_at >= :StartAt AND om.created_at < :EndAt) AS order_count,
                    (SELECT COUNT(1) FROM ORDER_MAIN om
                     WHERE om.created_at >= :StartAt AND om.created_at < :EndAt
                       AND om.status IN (1, 2, 3)) AS paid_count,
                    (SELECT NVL(SUM(om.pay_amount), 0) FROM ORDER_MAIN om
                     WHERE om.created_at >= :StartAt AND om.created_at < :EndAt
                       AND om.status IN (1, 2, 3)) AS sales_amount,
                    (SELECT COUNT(1) FROM "USER" u
                     WHERE u.created_at >= :StartAt AND u.created_at < :EndAt) AS new_user_count
                FROM DUAL
            ) source
            ON (target.stat_date = source.stat_date)
            WHEN MATCHED THEN UPDATE SET
                target.order_count = source.order_count,
                target.paid_count = source.paid_count,
                target.sales_amount = source.sales_amount,
                target.refund_amount = 0,
                target.avg_order_amount = CASE
                    WHEN source.paid_count = 0 THEN 0
                    ELSE ROUND(source.sales_amount / source.paid_count, 2)
                END,
                target.new_user_count = source.new_user_count
            WHEN NOT MATCHED THEN INSERT
                (stat_date, order_count, paid_count, sales_amount, refund_amount, avg_order_amount, new_user_count)
            VALUES
                (source.stat_date, source.order_count, source.paid_count, source.sales_amount, 0,
                 CASE WHEN source.paid_count = 0 THEN 0 ELSE ROUND(source.sales_amount / source.paid_count, 2) END,
                 source.new_user_count)
            """;
        command.Transaction = _unitOfWork.CurrentTransaction;
        if (command is OracleCommand oracleCommand)
        {
            oracleCommand.BindByName = true;
        }

        AddParameter(command, "StatDate", statDate);
        AddParameter(command, "StartAt", statDate);
        AddParameter(command, "EndAt", statDate.AddDays(1));

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
