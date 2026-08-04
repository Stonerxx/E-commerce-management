using Oracle.ManagedDataAccess.Client;

namespace ECommerce.OracleIntegrationTests;

public class OracleTransactionAndConstraintTests
{
    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Oracle_transaction_rollback_removes_fixture_and_binds_parameters()
    {
        var skuId = OracleTestEnvironment.NewId();
        await using var connection = await OracleTestEnvironment.OpenDevAsync();
        var references = await OracleTestEnvironment.GetSeedReferencesAsync(connection);
        await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync();

        await using (var insertSku = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO SKU (id, product_id, spec_desc, price, stock, locked_stock, warning_stock, status)
            VALUES (:Id, :ProductId, :SpecDesc, :Price, :Stock, 0, 0, 1)", transaction))
        {
            insertSku.Parameters.Add(":Id", OracleDbType.Int64).Value = skuId;
            insertSku.Parameters.Add(":ProductId", OracleDbType.Int64).Value = references.ProductId;
            insertSku.Parameters.Add(":SpecDesc", OracleDbType.Varchar2).Value = "{\"test\":\"rollback\"}";
            insertSku.Parameters.Add(":Price", OracleDbType.Decimal).Value = 1.25m;
            insertSku.Parameters.Add(":Stock", OracleDbType.Int32).Value = 2;
            Assert.Equal(1, await insertSku.ExecuteNonQueryAsync());
        }

        await using (var insertCart = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO CART (user_id, sku_id, quantity, selected)
            VALUES (:UserId, :SkuId, :Quantity, 1)", transaction))
        {
            insertCart.Parameters.Add(":UserId", OracleDbType.Int64).Value = references.UserId;
            insertCart.Parameters.Add(":SkuId", OracleDbType.Int64).Value = skuId;
            insertCart.Parameters.Add(":Quantity", OracleDbType.Int32).Value = 1;
            Assert.Equal(1, await insertCart.ExecuteNonQueryAsync());
        }

        await transaction.RollbackAsync();

        await using var verifyConnection = await OracleTestEnvironment.OpenDevAsync();
        await using var verify = OracleTestEnvironment.CreateCommand(verifyConnection, "SELECT COUNT(*) FROM SKU WHERE id = :Id");
        verify.Parameters.Add(":Id", OracleDbType.Int64).Value = skuId;
        Assert.Equal(0, Convert.ToInt32(await verify.ExecuteScalarAsync()));
    }

    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Oracle_rejects_zero_cart_and_order_item_quantities()
    {
        var skuId = OracleTestEnvironment.NewId();
        var orderId = skuId + 1;
        await using var connection = await OracleTestEnvironment.OpenDevAsync();
        var references = await OracleTestEnvironment.GetSeedReferencesAsync(connection);
        await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync();

        await InsertTemporarySkuAsync(connection, transaction, skuId, references.ProductId);

        await using (var invalidCart = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO CART (user_id, sku_id, quantity, selected)
            VALUES (:UserId, :SkuId, 0, 1)", transaction))
        {
            invalidCart.Parameters.Add(":UserId", OracleDbType.Int64).Value = references.UserId;
            invalidCart.Parameters.Add(":SkuId", OracleDbType.Int64).Value = skuId;
            var cartException = await Assert.ThrowsAsync<OracleException>(() => invalidCart.ExecuteNonQueryAsync());
            Assert.Equal(2290, cartException.Number);
        }

        await InsertTemporaryOrderAsync(connection, transaction, orderId, references.UserId, references.AddressId);
        await using (var invalidItem = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO ORDER_ITEM (order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal)
            VALUES (:OrderId, :SkuId, 'integration test', '{}', 'test.png', 1, 0, 0)", transaction))
        {
            invalidItem.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            invalidItem.Parameters.Add(":SkuId", OracleDbType.Int64).Value = skuId;
            var itemException = await Assert.ThrowsAsync<OracleException>(() => invalidItem.ExecuteNonQueryAsync());
            Assert.Equal(2290, itemException.Number);
        }

        await transaction.RollbackAsync();
    }

    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Oracle_rejects_invalid_order_amounts_and_status_transitions()
    {
        var orderId = OracleTestEnvironment.NewId();
        await using var connection = await OracleTestEnvironment.OpenDevAsync();
        var references = await OracleTestEnvironment.GetSeedReferencesAsync(connection);
        await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync();

        await InsertTemporaryOrderAsync(connection, transaction, orderId, references.UserId, references.AddressId);

        await using (var invalidPayment = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO PAYMENT (order_id, pay_method, status, pay_amount)
            VALUES (:OrderId, 'integration test', 0, 2)", transaction))
        {
            invalidPayment.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            var paymentException = await Assert.ThrowsAsync<OracleException>(() => invalidPayment.ExecuteNonQueryAsync());
            Assert.Equal(20012, paymentException.Number);
        }

        await using (var cancelledWithPayAmount = OracleTestEnvironment.CreateCommand(connection, @"
            UPDATE ORDER_MAIN
            SET status = 4
            WHERE id = :OrderId", transaction))
        {
            cancelledWithPayAmount.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            var amountException = await Assert.ThrowsAsync<OracleException>(() => cancelledWithPayAmount.ExecuteNonQueryAsync());
            Assert.Equal(2290, amountException.Number);
        }

        await using (var invalidTransition = OracleTestEnvironment.CreateCommand(connection, @"
            UPDATE ORDER_MAIN
            SET status = 3
            WHERE id = :OrderId", transaction))
        {
            invalidTransition.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            var transitionException = await Assert.ThrowsAsync<OracleException>(() => invalidTransition.ExecuteNonQueryAsync());
            Assert.Equal(20010, transitionException.Number);
        }

        await using (var cancelledOrder = OracleTestEnvironment.CreateCommand(connection, @"
            UPDATE ORDER_MAIN
            SET status = 4, pay_amount = 0
            WHERE id = :OrderId", transaction))
        {
            cancelledOrder.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            Assert.Equal(1, await cancelledOrder.ExecuteNonQueryAsync());
        }

        await transaction.RollbackAsync();
    }

    internal static async Task InsertTemporarySkuAsync(OracleConnection connection, OracleTransaction? transaction, long skuId, long productId)
    {
        await using var command = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO SKU (id, product_id, spec_desc, price, stock, locked_stock, warning_stock, status)
            VALUES (:Id, :ProductId, :SpecDesc, 1, 1, 0, 0, 1)", transaction);
        command.Parameters.Add(":Id", OracleDbType.Int64).Value = skuId;
        command.Parameters.Add(":ProductId", OracleDbType.Int64).Value = productId;
        command.Parameters.Add(":SpecDesc", OracleDbType.Varchar2).Value = $"{{\"test\":\"oracle-integration-{skuId}\"}}";
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
    }

    internal static async Task InsertTemporaryOrderAsync(OracleConnection connection, OracleTransaction? transaction, long orderId, long userId, long addressId)
    {
        await using var command = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO ORDER_MAIN (id, order_no, user_id, address_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, created_at, updated_at)
            VALUES (:Id, :OrderNo, :UserId, :AddressId, 0, 1, 0, 1, SYSDATE + 1, '{""source"":""oracle-integration""}', SYSDATE, SYSDATE)", transaction);
        command.Parameters.Add(":Id", OracleDbType.Int64).Value = orderId;
        command.Parameters.Add(":OrderNo", OracleDbType.Varchar2).Value = $"ORACLE-IT-{orderId}";
        command.Parameters.Add(":UserId", OracleDbType.Int64).Value = userId;
        command.Parameters.Add(":AddressId", OracleDbType.Int64).Value = addressId;
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
    }
}
