using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.OracleIntegrationTests;

public sealed class OracleMember5ModuleTests
{
    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Coupon_inventory_unique_receive_and_single_use_are_enforced()
    {
        await using var connection = await OracleTestEnvironment.OpenDevAsync();
        var references = await OracleTestEnvironment.GetSeedReferencesAsync(connection);
        await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync();
        int templateId;
        long userCouponId;

        await using (var insertTemplate = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO COUPON_TEMPLATE
                (name, type, amount, min_amount, total_count, received_count, start_time, end_time, status)
            VALUES
                (:Name, 1, 10, 100, 1, 0, SYSDATE - 1, SYSDATE + 1, 1)
            RETURNING id INTO :Id", transaction))
        {
            insertTemplate.Parameters.Add(":Name", OracleDbType.Varchar2).Value = $"oracle coupon {OracleTestEnvironment.NewId()}";
            var id = new OracleParameter(":Id", OracleDbType.Int32) { Direction = ParameterDirection.Output };
            insertTemplate.Parameters.Add(id);
            Assert.Equal(1, await insertTemplate.ExecuteNonQueryAsync());
            templateId = Convert.ToInt32(id.Value);
        }

        await using (var receiveInventory = OracleTestEnvironment.CreateCommand(connection, @"
            UPDATE COUPON_TEMPLATE
            SET received_count = received_count + 1
            WHERE id = :TemplateId
              AND status = 1
              AND start_time <= SYSDATE
              AND end_time >= SYSDATE
              AND (total_count = -1 OR received_count < total_count)", transaction))
        {
            receiveInventory.Parameters.Add(":TemplateId", OracleDbType.Int32).Value = templateId;
            Assert.Equal(1, await receiveInventory.ExecuteNonQueryAsync());
            Assert.Equal(0, await receiveInventory.ExecuteNonQueryAsync());
        }

        await using (var insertUserCoupon = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO USER_COUPON (user_id, coupon_template_id, status, received_at)
            VALUES (:UserId, :TemplateId, 0, SYSDATE)
            RETURNING id INTO :Id", transaction))
        {
            insertUserCoupon.Parameters.Add(":UserId", OracleDbType.Int64).Value = references.UserId;
            insertUserCoupon.Parameters.Add(":TemplateId", OracleDbType.Int32).Value = templateId;
            var id = new OracleParameter(":Id", OracleDbType.Int64) { Direction = ParameterDirection.Output };
            insertUserCoupon.Parameters.Add(id);
            Assert.Equal(1, await insertUserCoupon.ExecuteNonQueryAsync());
            userCouponId = Convert.ToInt64(id.Value);
        }

        await using (var duplicate = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO USER_COUPON (user_id, coupon_template_id, status, received_at)
            VALUES (:UserId, :TemplateId, 0, SYSDATE)", transaction))
        {
            duplicate.Parameters.Add(":UserId", OracleDbType.Int64).Value = references.UserId;
            duplicate.Parameters.Add(":TemplateId", OracleDbType.Int32).Value = templateId;
            var exception = await Assert.ThrowsAsync<OracleException>(() => duplicate.ExecuteNonQueryAsync());
            Assert.Equal(1, exception.Number);
        }

        var orderId = OracleTestEnvironment.NewId();
        await using (var useCoupon = OracleTestEnvironment.CreateCommand(connection, @"
            UPDATE USER_COUPON
            SET status = 1, used_at = SYSDATE, order_id = :OrderId
            WHERE id = :UserCouponId
              AND user_id = :UserId
              AND status = 0
              AND EXISTS (
                  SELECT 1 FROM COUPON_TEMPLATE ct
                  WHERE ct.id = USER_COUPON.coupon_template_id
                    AND ct.status = 1
                    AND ct.min_amount <= 100
                    AND ct.amount = 10)", transaction))
        {
            useCoupon.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            useCoupon.Parameters.Add(":UserCouponId", OracleDbType.Int64).Value = userCouponId;
            useCoupon.Parameters.Add(":UserId", OracleDbType.Int64).Value = references.UserId;
            Assert.Equal(1, await useCoupon.ExecuteNonQueryAsync());
            Assert.Equal(0, await useCoupon.ExecuteNonQueryAsync());
        }

        await transaction.RollbackAsync();

        await using var verify = OracleTestEnvironment.CreateCommand(
            connection,
            "SELECT COUNT(*) FROM COUPON_TEMPLATE WHERE id = :TemplateId");
        verify.Parameters.Add(":TemplateId", OracleDbType.Int32).Value = templateId;
        Assert.Equal(0, Convert.ToInt32(await verify.ExecuteScalarAsync()));
    }

    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Review_supports_clob_json_and_rejects_duplicate_or_invalid_images()
    {
        var skuId = OracleTestEnvironment.NewId();
        var orderId = skuId + 1;
        await using var connection = await OracleTestEnvironment.OpenDevAsync();
        var references = await OracleTestEnvironment.GetSeedReferencesAsync(connection);
        await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync();

        await OracleTransactionAndConstraintTests.InsertTemporarySkuAsync(
            connection,
            transaction,
            skuId,
            references.ProductId);
        await OracleTransactionAndConstraintTests.InsertTemporaryOrderAsync(
            connection,
            transaction,
            orderId,
            references.UserId,
            references.AddressId);

        await using (var completeOrder = OracleTestEnvironment.CreateCommand(
            connection,
            "UPDATE ORDER_MAIN SET status = 3 WHERE id = :OrderId",
            transaction))
        {
            completeOrder.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            Assert.Equal(1, await completeOrder.ExecuteNonQueryAsync());
        }

        await using (var insertItem = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO ORDER_ITEM
                (order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal)
            VALUES
                (:OrderId, :SkuId, 'review integration', '{}', 'review.png', 1, 1, 1)", transaction))
        {
            insertItem.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            insertItem.Parameters.Add(":SkuId", OracleDbType.Int64).Value = skuId;
            Assert.Equal(1, await insertItem.ExecuteNonQueryAsync());
        }

        var content = new string('评', 5000);
        long reviewId;
        await using (var insertReview = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO REVIEW
                (order_id, product_id, user_id, rating, content, images, is_anonymous, status, created_at)
            VALUES
                (:OrderId, :ProductId, :UserId, 5, :Content, '[""review.png""]', 1, 0, SYSDATE)
            RETURNING id INTO :Id", transaction))
        {
            insertReview.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            insertReview.Parameters.Add(":ProductId", OracleDbType.Int64).Value = references.ProductId;
            insertReview.Parameters.Add(":UserId", OracleDbType.Int64).Value = references.UserId;
            insertReview.Parameters.Add(":Content", OracleDbType.Clob).Value = content;
            var id = new OracleParameter(":Id", OracleDbType.Int64) { Direction = ParameterDirection.Output };
            insertReview.Parameters.Add(id);
            Assert.Equal(1, await insertReview.ExecuteNonQueryAsync());
            reviewId = Convert.ToInt64(id.Value);
        }

        await using (var readReview = OracleTestEnvironment.CreateCommand(connection, @"
            SELECT DBMS_LOB.GETLENGTH(content), images, created_at
            FROM REVIEW
            WHERE id = :ReviewId", transaction))
        {
            readReview.Parameters.Add(":ReviewId", OracleDbType.Int64).Value = reviewId;
            await using var reader = await readReview.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(content.Length, reader.GetInt32(0));
            Assert.Equal("[\"review.png\"]", reader.GetString(1));
            Assert.False(reader.IsDBNull(2));
        }

        await using (var duplicate = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO REVIEW (order_id, product_id, user_id, rating, images, is_anonymous, status)
            VALUES (:OrderId, :ProductId, :UserId, 5, '[]', 0, 0)", transaction))
        {
            duplicate.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            duplicate.Parameters.Add(":ProductId", OracleDbType.Int64).Value = references.ProductId;
            duplicate.Parameters.Add(":UserId", OracleDbType.Int64).Value = references.UserId;
            var exception = await Assert.ThrowsAsync<OracleException>(() => duplicate.ExecuteNonQueryAsync());
            Assert.Equal(1, exception.Number);
        }

        await using (var deleteReview = OracleTestEnvironment.CreateCommand(
            connection,
            "DELETE FROM REVIEW WHERE id = :ReviewId",
            transaction))
        {
            deleteReview.Parameters.Add(":ReviewId", OracleDbType.Int64).Value = reviewId;
            Assert.Equal(1, await deleteReview.ExecuteNonQueryAsync());
        }

        await using (var invalidJson = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO REVIEW (order_id, product_id, user_id, rating, images, is_anonymous, status)
            VALUES (:OrderId, :ProductId, :UserId, 5, 'not-json', 0, 0)", transaction))
        {
            invalidJson.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            invalidJson.Parameters.Add(":ProductId", OracleDbType.Int64).Value = references.ProductId;
            invalidJson.Parameters.Add(":UserId", OracleDbType.Int64).Value = references.UserId;
            var exception = await Assert.ThrowsAsync<OracleException>(() => invalidJson.ExecuteNonQueryAsync());
            Assert.Equal(2290, exception.Number);
        }

        await transaction.RollbackAsync();
    }

    [DevOracleFact]
    [Trait("Category", "OracleIntegration")]
    public async Task Shipment_order_logs_and_audit_roll_back_as_one_transaction()
    {
        var orderId = OracleTestEnvironment.NewId();
        await using var connection = await OracleTestEnvironment.OpenDevAsync();
        var references = await OracleTestEnvironment.GetSeedReferencesAsync(connection);
        await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync();

        await OracleTransactionAndConstraintTests.InsertTemporaryOrderAsync(
            connection,
            transaction,
            orderId,
            references.UserId,
            references.AddressId);
        await using (var markPaid = OracleTestEnvironment.CreateCommand(
            connection,
            "UPDATE ORDER_MAIN SET status = 1 WHERE id = :OrderId",
            transaction))
        {
            markPaid.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            Assert.Equal(1, await markPaid.ExecuteNonQueryAsync());
        }

        long logisticsId;
        await using (var insertLogistics = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO LOGISTICS (order_id, company_name, tracking_no, shipped_at, status)
            VALUES (:OrderId, 'oracle logistics', :TrackingNo, SYSDATE, 0)
            RETURNING id INTO :Id", transaction))
        {
            insertLogistics.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            insertLogistics.Parameters.Add(":TrackingNo", OracleDbType.Varchar2).Value = $"ORACLE-{orderId}";
            var id = new OracleParameter(":Id", OracleDbType.Int64) { Direction = ParameterDirection.Output };
            insertLogistics.Parameters.Add(id);
            Assert.Equal(1, await insertLogistics.ExecuteNonQueryAsync());
            logisticsId = Convert.ToInt64(id.Value);
        }

        await using (var insertTrack = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO LOGISTICS_TRACK (logistics_id, track_desc, track_time)
            VALUES (:LogisticsId, 'collected', SYSDATE)", transaction))
        {
            insertTrack.Parameters.Add(":LogisticsId", OracleDbType.Int64).Value = logisticsId;
            Assert.Equal(1, await insertTrack.ExecuteNonQueryAsync());
        }

        await using (var shipOrder = OracleTestEnvironment.CreateCommand(connection, @"
            UPDATE ORDER_MAIN SET status = 2, updated_at = SYSDATE
            WHERE id = :OrderId AND status = 1", transaction))
        {
            shipOrder.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            Assert.Equal(1, await shipOrder.ExecuteNonQueryAsync());
        }

        await using (var orderLog = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO ORDER_LOG
                (order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
            VALUES
                (:OrderId, 1, 2, :OperatorId, 'oracle-test', 'shipment', SYSDATE)", transaction))
        {
            orderLog.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
            orderLog.Parameters.Add(":OperatorId", OracleDbType.Int64).Value = references.UserId;
            Assert.Equal(1, await orderLog.ExecuteNonQueryAsync());
        }

        await using (var audit = OracleTestEnvironment.CreateCommand(connection, @"
            INSERT INTO OPERATION_LOG
                (operator_id, operator_name, module, action, description, ip_address, result, created_at)
            VALUES
                (:OperatorId, 'oracle-test', 'logistics', 'ship', 'shipment', '127.0.0.1', 1, SYSDATE)", transaction))
        {
            audit.Parameters.Add(":OperatorId", OracleDbType.Int64).Value = references.UserId;
            Assert.Equal(1, await audit.ExecuteNonQueryAsync());
        }

        await transaction.RollbackAsync();

        await using var verify = OracleTestEnvironment.CreateCommand(
            connection,
            "SELECT COUNT(*) FROM ORDER_MAIN WHERE id = :OrderId");
        verify.Parameters.Add(":OrderId", OracleDbType.Int64).Value = orderId;
        Assert.Equal(0, Convert.ToInt32(await verify.ExecuteScalarAsync()));
    }
}
