using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.OracleIntegrationTests;

public class OracleConcurrencyTests
{
    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Stock_lock_is_atomic_when_two_requests_compete_for_one_unit()
    {
        var skuId = OracleTestEnvironment.NewId();
        await using var setupConnection = await OracleTestEnvironment.OpenDevAsync();
        var references = await OracleTestEnvironment.GetSeedReferencesAsync(setupConnection);
        await OracleTransactionAndConstraintTests.InsertTemporarySkuAsync(setupConnection, null, skuId, references.ProductId);

        try
        {
            var results = await Task.WhenAll(
                TryLockOneUnitAsync(skuId),
                TryLockOneUnitAsync(skuId));

            Assert.Equal(1, results.Count(result => result));

            await using var verify = OracleTestEnvironment.CreateCommand(setupConnection, "SELECT locked_stock FROM SKU WHERE id = :Id");
            verify.Parameters.Add(":Id", OracleDbType.Int64).Value = skuId;
            Assert.Equal(1, Convert.ToInt32(await verify.ExecuteScalarAsync()));
        }
        finally
        {
            await using var cleanup = OracleTestEnvironment.CreateCommand(setupConnection, "DELETE FROM SKU WHERE id = :Id");
            cleanup.Parameters.Add(":Id", OracleDbType.Int64).Value = skuId;
            await cleanup.ExecuteNonQueryAsync();
        }
    }

    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Conditional_order_status_update_allows_only_one_payment_or_cancellation_winner()
    {
        var orderId = OracleTestEnvironment.NewId();
        await using var setupConnection = await OracleTestEnvironment.OpenDevAsync();
        var references = await OracleTestEnvironment.GetSeedReferencesAsync(setupConnection);
        await OracleTransactionAndConstraintTests.InsertTemporaryOrderAsync(setupConnection, null, orderId, references.UserId, references.AddressId);

        try
        {
            var results = await Task.WhenAll(
                TryUpdateStatusAsync(orderId, targetStatus: 1),
                TryUpdateStatusAsync(orderId, targetStatus: 4));

            Assert.Equal(1, results.Count(result => result));
            Assert.False(await TryUpdateStatusAsync(orderId, targetStatus: 1), "A duplicate payment/cancellation request must not transition a non-pending order.");
        }
        finally
        {
            await using var cleanup = OracleTestEnvironment.CreateCommand(setupConnection, "DELETE FROM ORDER_MAIN WHERE id = :Id");
            cleanup.Parameters.Add(":Id", OracleDbType.Int64).Value = orderId;
            await cleanup.ExecuteNonQueryAsync();
        }
    }

    private static async Task<bool> TryLockOneUnitAsync(long skuId)
    {
        await using var connection = await OracleTestEnvironment.OpenDevAsync();
        await using var command = OracleTestEnvironment.CreateCommand(connection, @"
            UPDATE SKU
            SET locked_stock = locked_stock + 1
            WHERE id = :Id AND stock - locked_stock >= 1");
        command.Parameters.Add(":Id", OracleDbType.Int64).Value = skuId;
        return await command.ExecuteNonQueryAsync() == 1;
    }

    private static async Task<bool> TryUpdateStatusAsync(long orderId, int targetStatus)
    {
        await using var unitOfWork = new UnitOfWork(new TestOracleConnectionFactory(OracleTestEnvironment.DevConnectionString!));
        var repository = new OrderRepository(unitOfWork);
        return await repository.TryUpdateStatusAsync(orderId, expectedStatus: 0, targetStatus, DateTime.UtcNow);
    }
}
