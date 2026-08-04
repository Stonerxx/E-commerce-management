using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.OracleIntegrationTests;

public class OracleDatabaseObjectTests
{
    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Dev_schema_contains_valid_database_objects()
    {
        await using var connection = await OracleTestEnvironment.OpenDevAsync();
        await using var command = OracleTestEnvironment.CreateCommand(connection, @"
            SELECT COUNT(*)
            FROM USER_OBJECTS
            WHERE STATUS = 'VALID'
              AND (OBJECT_NAME, OBJECT_TYPE) IN (
                  ('FN_AVAILABLE_STOCK', 'FUNCTION'),
                  ('SP_REFRESH_ORDER_STAT_SNAPSHOT', 'PROCEDURE'),
                  ('V_PRODUCT_INVENTORY', 'VIEW'),
                  ('V_ORDER_REPORT', 'VIEW'),
                  ('TRG_ORDER_ADDRESS_OWNER_GUARD', 'TRIGGER'),
                  ('TRG_PAYMENT_AMOUNT_GUARD', 'TRIGGER'),
                  ('TRG_ORDER_STATUS_FLOW_GUARD', 'TRIGGER'),
                  ('TRG_ORDER_PAID_UPDATE_SALES', 'TRIGGER'))");

        Assert.Equal(8, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }

    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Available_stock_function_and_inventory_view_return_consistent_values()
    {
        var skuId = OracleTestEnvironment.NewId();
        await using var connection = await OracleTestEnvironment.OpenDevAsync();
        var references = await OracleTestEnvironment.GetSeedReferencesAsync(connection);
        await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync();

        await OracleTransactionAndConstraintTests.InsertTemporarySkuAsync(connection, transaction, skuId, references.ProductId);

        await using (var update = OracleTestEnvironment.CreateCommand(connection, @"
            UPDATE SKU
            SET stock = 10, locked_stock = 3, warning_stock = 7
            WHERE id = :SkuId", transaction))
        {
            update.Parameters.Add(":SkuId", OracleDbType.Int64).Value = skuId;
            Assert.Equal(1, await update.ExecuteNonQueryAsync());
        }

        await using (var functionCommand = OracleTestEnvironment.CreateCommand(
                         connection,
                         "SELECT FN_AVAILABLE_STOCK(:SkuId) FROM DUAL",
                         transaction))
        {
            functionCommand.Parameters.Add(":SkuId", OracleDbType.Int64).Value = skuId;
            Assert.Equal(7, Convert.ToInt32(await functionCommand.ExecuteScalarAsync()));
        }

        await using (var viewCommand = OracleTestEnvironment.CreateCommand(connection, @"
            SELECT available_stock, is_warning
            FROM V_PRODUCT_INVENTORY
            WHERE sku_id = :SkuId", transaction))
        {
            viewCommand.Parameters.Add(":SkuId", OracleDbType.Int64).Value = skuId;
            await using var reader = await viewCommand.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(7, reader.GetInt32(0));
            Assert.Equal(1, reader.GetInt32(1));
        }

        await transaction.RollbackAsync();
    }

    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Statistics_snapshot_procedure_uses_the_caller_transaction()
    {
        var statDate = new DateTime(2099, 1, 1);
        await using var connection = await OracleTestEnvironment.OpenDevAsync();
        await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync();

        await using (var procedure = OracleTestEnvironment.CreateCommand(connection, "SP_REFRESH_ORDER_STAT_SNAPSHOT", transaction))
        {
            procedure.CommandType = CommandType.StoredProcedure;
            procedure.Parameters.Add("p_stat_date", OracleDbType.Date).Value = statDate;
            await procedure.ExecuteNonQueryAsync();
        }

        await using (var verify = OracleTestEnvironment.CreateCommand(connection, @"
            SELECT order_count, paid_count, sales_amount
            FROM ORDER_STAT_SNAPSHOT
            WHERE stat_date = :StatDate", transaction))
        {
            verify.Parameters.Add(":StatDate", OracleDbType.Date).Value = statDate;
            await using var reader = await verify.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(0, reader.GetInt32(0));
            Assert.Equal(0, reader.GetInt32(1));
            Assert.Equal(0m, reader.GetDecimal(2));
        }

        await transaction.RollbackAsync();
    }

    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Paid_order_trigger_updates_product_sales_once()
    {
        var skuId = OracleTestEnvironment.NewId();
        var orderId = skuId + 1;
        await using var connection = await OracleTestEnvironment.OpenDevAsync();
        var references = await OracleTestEnvironment.GetSeedReferencesAsync(connection);
        await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync();

        await OracleTransactionAndConstraintTests.InsertTemporarySkuAsync(connection, transaction, skuId, references.ProductId);
        await OracleTransactionAndConstraintTests.InsertTemporaryOrderAsync(connection, transaction, orderId, references.UserId, references.AddressId);

        await using (var item = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO ORDER_ITEM (order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal)
            VALUES (:OrderId, :SkuId, 'trigger test', '{}', 'test.png', 1, 2, 2)", transaction))
        {
            item.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            item.Parameters.Add(":SkuId", OracleDbType.Int64).Value = skuId;
            Assert.Equal(1, await item.ExecuteNonQueryAsync());
        }

        var salesBefore = await GetSalesCountAsync(connection, transaction, references.ProductId);

        await using (var pay = OracleTestEnvironment.CreateCommand(connection, @"
            UPDATE ORDER_MAIN
            SET status = 1
            WHERE id = :OrderId AND status = 0", transaction))
        {
            pay.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            Assert.Equal(1, await pay.ExecuteNonQueryAsync());
        }

        Assert.Equal(salesBefore + 2, await GetSalesCountAsync(connection, transaction, references.ProductId));

        await transaction.RollbackAsync();
    }

    private static async Task<int> GetSalesCountAsync(
        OracleConnection connection,
        OracleTransaction transaction,
        long productId)
    {
        await using var command = OracleTestEnvironment.CreateCommand(
            connection,
            "SELECT sales_count FROM PRODUCT WHERE id = :ProductId",
            transaction);
        command.Parameters.Add(":ProductId", OracleDbType.Int64).Value = productId;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}
