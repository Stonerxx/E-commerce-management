using ClosedXML.Excel;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.OracleIntegrationTests;

public class OracleExcelVolumeTests
{
    [LongRunningDevOracleFact]
    [Trait("Category", "OracleIntegration")]
    [Trait("Category", "LongRunning")]
    public async Task Order_export_returns_a_valid_5000_row_workbook()
    {
        const int rowCount = 5_000;
        var firstOrderId = OracleTestEnvironment.NewId();
        var shortToken = (firstOrderId % 1_000_000_000_000L).ToString("D12");
        var prefix = $"ORACLE-EXPORT-{shortToken}";
        Assert.True($"{prefix}-0000".Length <= 32, "ORDER_MAIN.ORDER_NO is limited to VARCHAR2(32).");
        await using var connection = await OracleTestEnvironment.OpenDevAsync();
        var references = await OracleTestEnvironment.GetSeedReferencesAsync(connection);

        try
        {
            await InsertOrdersAsync(connection, firstOrderId, rowCount, prefix, references.UserId, references.AddressId);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Oracle:ConnectionString"] = OracleTestEnvironment.DevConnectionString
                })
                .Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(configuration);
            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var exportService = scope.ServiceProvider.GetRequiredService<IExportService>();

            var export = await exportService.ExportOrdersAsync(new AdminOrderQuery
            {
                OrderNo = prefix
            });

            await using var stream = new MemoryStream(export.Content);
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet("Orders");
            Assert.Equal(rowCount + 1, worksheet.LastRowUsed()!.RowNumber());
            Assert.Equal("订单编号", worksheet.Cell(1, 1).GetString());
        }
        finally
        {
            await using var cleanup = OracleTestEnvironment.CreateCommand(connection, "DELETE FROM ORDER_MAIN WHERE id >= :FirstId AND id < :EndId");
            cleanup.Parameters.Add(":FirstId", OracleDbType.Int64).Value = firstOrderId;
            cleanup.Parameters.Add(":EndId", OracleDbType.Int64).Value = firstOrderId + rowCount;
            await cleanup.ExecuteNonQueryAsync();
        }
    }

    private static async Task InsertOrdersAsync(
        OracleConnection connection,
        long firstOrderId,
        int rowCount,
        string prefix,
        long userId,
        long addressId)
    {
        await using var command = OracleTestEnvironment.CreateCommand(connection, """
            INSERT INTO ORDER_MAIN
                (id, order_no, user_id, address_id, status, total_amount, discount_amount,
                 pay_amount, pay_expire_time, receiver_snapshot, created_at, updated_at)
            SELECT
                :FirstOrderId + generated.row_index - 1,
                :Prefix || '-' || LPAD(generated.row_index - 1, 4, '0'),
                :UserId,
                :AddressId,
                1,
                1,
                0,
                1,
                SYSDATE + 1,
                '{"source":"oracle-integration"}',
                SYSDATE,
                SYSDATE
            FROM (
                SELECT LEVEL AS row_index
                FROM DUAL
                CONNECT BY LEVEL <= :RowCount
            ) generated
            """);
        command.CommandTimeout = 180;
        command.Parameters.Add(":FirstOrderId", OracleDbType.Int64).Value = firstOrderId;
        command.Parameters.Add(":Prefix", OracleDbType.Varchar2).Value = prefix;
        command.Parameters.Add(":UserId", OracleDbType.Int64).Value = userId;
        command.Parameters.Add(":AddressId", OracleDbType.Int64).Value = addressId;
        command.Parameters.Add(":RowCount", OracleDbType.Int32).Value = rowCount;
        Assert.Equal(rowCount, await command.ExecuteNonQueryAsync());
    }
}
