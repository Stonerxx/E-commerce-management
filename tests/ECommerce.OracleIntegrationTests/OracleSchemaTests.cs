using ECommerce.Infrastructure.Data;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.OracleIntegrationTests;

public class OracleSchemaTests
{
    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Generated_user_id_uses_the_post_seed_range()
    {
        var token = OracleTestEnvironment.NewId();
        var username = $"oracle_it_{token}";
        long userId = 0;
        await using var connection = await OracleTestEnvironment.OpenDevAsync();
        try
        {
            await using var insert = OracleTestEnvironment.CreateCommand(connection, @"
                INSERT INTO ""USER"" (username, password_hash, status)
                VALUES (:Username, 'NOT_A_LOGIN_PASSWORD', 1)
                RETURNING id INTO :Id");
            insert.Parameters.Add(":Username", OracleDbType.Varchar2).Value = username;
            var idParameter = new OracleParameter(":Id", OracleDbType.Int64) { Direction = System.Data.ParameterDirection.Output };
            insert.Parameters.Add(idParameter);
            await insert.ExecuteNonQueryAsync();
            userId = OracleValueConverter.ToInt64(idParameter.Value);
            Assert.True(userId >= 10_001, $"Expected generated user id >= 10001, got {userId}.");
        }
        finally
        {
            await using var cleanup = OracleTestEnvironment.CreateCommand(connection, "DELETE FROM \"USER\" WHERE username = :Username");
            cleanup.Parameters.Add(":Username", OracleDbType.Varchar2).Value = username;
            await cleanup.ExecuteNonQueryAsync();
        }
    }

    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Dev_schema_contains_core_tables_and_quantity_constraints()
    {
        await using var connection = await OracleTestEnvironment.OpenDevAsync();
        await using var command = OracleTestEnvironment.CreateCommand(connection, @"
            SELECT COUNT(*)
            FROM USER_TABLES
            WHERE TABLE_NAME IN ('USER', 'SKU', 'CART', 'ORDER_MAIN', 'ORDER_ITEM', 'ORDER_STAT_SNAPSHOT')");

        var tableCount = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(6, tableCount);

        command.CommandText = @"
            SELECT COUNT(*)
            FROM USER_CONSTRAINTS
            WHERE CONSTRAINT_NAME IN (
                'CH_CART_QUANTITY',
                'CH_ORDER_ITEM_QUANTITY',
                'CH_SKU_STOCK_NONNEGATIVE',
                'CH_SKU_LOCKED_STOCK_NONNEGATIVE',
                'CH_SKU_STOCK_NOT_BELOW_LOCKED',
                'UK_SKU_PRODUCT_SPEC',
                'CH_OM_AMOUNT_CONSISTENCY',
                'CH_ORDER_ITEM_AMOUNT',
                'CH_PAY_AMOUNT_NONNEGATIVE',
                'CH_PAY_PAID_AT',
                'CH_PAY_TRADE_NO')
              AND STATUS = 'ENABLED'";
        var constraintCount = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(11, constraintCount);

        command.CommandText = """SELECT COUNT(*) FROM "USER" WHERE id = 1 AND username = 'system' AND status = 0""";
        Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync()));

        command.CommandText = @"
            SELECT COUNT(*)
            FROM USER_INDEXES
            WHERE INDEX_NAME = 'UK_ADDRESS_ONE_DEFAULT'
              AND UNIQUENESS = 'UNIQUE'";
        Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }

    [DemoOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Demo_database_contains_seed_baseline_without_writing()
    {
        await using var connection = await OracleTestEnvironment.OpenDemoAsync();
        await using var command = OracleTestEnvironment.CreateCommand(connection, @"
            SELECT
                (SELECT COUNT(*) FROM ""USER"" WHERE id BETWEEN 9001 AND 9010) AS user_count,
                (SELECT COUNT(*) FROM PRODUCT WHERE id BETWEEN 9001 AND 9240) AS product_count,
                (SELECT COUNT(*) FROM SKU WHERE id BETWEEN 9001 AND 9240) AS sku_count,
                (SELECT COUNT(*) FROM ORDER_MAIN WHERE id BETWEEN 9000 AND 9999) AS order_count,
                (SELECT COUNT(*) FROM ORDER_MAIN
                 WHERE id BETWEEN 9000 AND 9999
                   AND status = 4
                   AND pay_amount <> 0) AS cancelled_order_amount_errors,
                (SELECT COUNT(*) FROM ORDER_MAIN om
                 WHERE om.id BETWEEN 9000 AND 9999
                   AND om.total_amount <> (
                       SELECT NVL(SUM(oi.subtotal), 0)
                       FROM ORDER_ITEM oi
                       WHERE oi.order_id = om.id
                   )) AS order_item_amount_errors
            FROM DUAL");

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(10, reader.GetInt32(0));
        Assert.Equal(150, reader.GetInt32(1));
        Assert.Equal(156, reader.GetInt32(2));
        Assert.Equal(48, reader.GetInt32(3));
        Assert.Equal(0, reader.GetInt32(4));
        Assert.Equal(0, reader.GetInt32(5));
    }

    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Dev_schema_contains_coupon_and_review_concurrency_constraints()
    {
        await using var connection = await OracleTestEnvironment.OpenDevAsync();
        await using var command = OracleTestEnvironment.CreateCommand(connection, @"
            SELECT COUNT(*)
            FROM USER_CONSTRAINTS
            WHERE CONSTRAINT_NAME IN (
                'UK_UC_USER_TEMPLATE',
                'UK_REVIEW_ORDER_PRODUCT_USER',
                'CH_COUP_AMOUNT',
                'CH_COUP_TOTAL',
                'CH_COUP_RECEIVED')
              AND STATUS = 'ENABLED'");

        Assert.Equal(5, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }
}
