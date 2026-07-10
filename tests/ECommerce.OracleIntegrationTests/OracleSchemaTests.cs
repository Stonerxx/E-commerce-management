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
            userId = Convert.ToInt64(idParameter.Value);
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
                'CH_SKU_STOCK_NOT_BELOW_LOCKED')
              AND STATUS = 'ENABLED'";
        var constraintCount = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(5, constraintCount);

        command.CommandText = """SELECT COUNT(*) FROM "USER" WHERE id = 1 AND username = 'system' AND status = 0""";
        Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }

    [DemoOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Demo_database_contains_seed_baseline_without_writing()
    {
        await using var connection = await OracleTestEnvironment.OpenDemoAsync();
        await using var command = OracleTestEnvironment.CreateCommand(connection, @"
            SELECT
                (SELECT COUNT(*) FROM ""USER"" WHERE id = 9001) AS user_count,
                (SELECT COUNT(*) FROM PRODUCT WHERE id = 9001) AS product_count,
                (SELECT COUNT(*) FROM ORDER_MAIN WHERE id = 9001) AS order_count
            FROM DUAL");

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal(1, reader.GetInt32(1));
        Assert.Equal(1, reader.GetInt32(2));
    }
}
