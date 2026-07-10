using Oracle.ManagedDataAccess.Client;

namespace ECommerce.OracleIntegrationTests;

public class OracleSchemaTests
{
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
            WHERE CONSTRAINT_NAME IN ('CH_CART_QUANTITY', 'CH_ORDER_ITEM_QUANTITY')
              AND STATUS = 'ENABLED'";
        var constraintCount = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(2, constraintCount);
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
