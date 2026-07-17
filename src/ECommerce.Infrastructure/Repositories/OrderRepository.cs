using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;
using System.Text;

namespace ECommerce.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private DbConnection Connection => _unitOfWork.CurrentConnection ?? throw new InvalidOperationException("Connection not opened. Call GetOpenConnectionAsync first.");
    private DbTransaction? Transaction => _unitOfWork.CurrentTransaction;

    // 写入
    public async Task<long> InsertOrderMainAsync(OrderMain order, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO order_main 
                (order_no, user_id, address_id, user_coupon_id, status, 
                 total_amount, discount_amount, pay_amount, pay_expire_time, 
                 receiver_snapshot, remark, created_at, updated_at)
            VALUES 
                (:OrderNo, :UserId, :AddressId, :UserCouponId, :Status,
                 :TotalAmount, :DiscountAmount, :PayAmount, :PayExpireTime,
                 :ReceiverSnapshot, :Remark, :CreatedAt, :UpdatedAt)
            RETURNING id INTO :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        cmd.Parameters.Add(CreateParameter("OrderNo", order.OrderNo));
        cmd.Parameters.Add(CreateParameter("UserId", order.UserId));
        cmd.Parameters.Add(CreateParameter("AddressId", order.AddressId));
        cmd.Parameters.Add(CreateParameter("UserCouponId", order.UserCouponId));
        cmd.Parameters.Add(CreateParameter("Status", order.Status));
        cmd.Parameters.Add(CreateParameter("TotalAmount", order.TotalAmount));
        cmd.Parameters.Add(CreateParameter("DiscountAmount", order.DiscountAmount));
        cmd.Parameters.Add(CreateParameter("PayAmount", order.PayAmount));
        cmd.Parameters.Add(CreateParameter("PayExpireTime", order.PayExpireTime));
        cmd.Parameters.Add(CreateParameter("ReceiverSnapshot", order.ReceiverSnapshot));
        cmd.Parameters.Add(CreateParameter("Remark", order.Remark));
        cmd.Parameters.Add(CreateParameter("CreatedAt", order.CreatedAt));
        cmd.Parameters.Add(CreateParameter("UpdatedAt", order.UpdatedAt));

        var pId = cmd.CreateParameter();
        pId.ParameterName = "Id";
        pId.DbType = DbType.Int64;
        pId.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(pId);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        order.Id = Convert.ToInt64(pId.Value);
        return order.Id;
    }

    public async Task InsertOrderItemsAsync(IEnumerable<OrderItem> items, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO order_item 
                (order_id, sku_id, product_name_snap, spec_snap, main_image_snap, 
                 unit_price, quantity, subtotal)
            VALUES 
                (:OrderId, :SkuId, :ProductNameSnap, :SpecSnap, :MainImageSnap,
                 :UnitPrice, :Quantity, :Subtotal)";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        // 为每个 Item 添加参数并执行
        foreach (var item in items)
        {
            cmd.Parameters.Clear();
            cmd.Parameters.Add(CreateParameter("OrderId", item.OrderId));
            cmd.Parameters.Add(CreateParameter("SkuId", item.SkuId));
            cmd.Parameters.Add(CreateParameter("ProductNameSnap", item.ProductNameSnap));
            cmd.Parameters.Add(CreateParameter("SpecSnap", item.SpecSnap));
            cmd.Parameters.Add(CreateParameter("MainImageSnap", item.MainImageSnap));
            cmd.Parameters.Add(CreateParameter("UnitPrice", item.UnitPrice));
            cmd.Parameters.Add(CreateParameter("Quantity", item.Quantity));
            cmd.Parameters.Add(CreateParameter("Subtotal", item.Subtotal));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task InsertOrderLogAsync(OrderLog log, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO order_log 
                (order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
            VALUES 
                (:OrderId, :FromStatus, :ToStatus, :OperatorId, :OperatorName, :Remark, :CreatedAt)";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("OrderId", log.OrderId));
        cmd.Parameters.Add(CreateParameter("FromStatus", log.FromStatus));
        cmd.Parameters.Add(CreateParameter("ToStatus", log.ToStatus));
        cmd.Parameters.Add(CreateParameter("OperatorId", log.OperatorId));
        cmd.Parameters.Add(CreateParameter("OperatorName", log.OperatorName));
        cmd.Parameters.Add(CreateParameter("Remark", log.Remark));
        cmd.Parameters.Add(CreateParameter("CreatedAt", log.CreatedAt));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    // 更新
    public async Task<bool> TryUpdateStatusAsync(
        long orderId,
        int expectedStatus,
        int targetStatus,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE order_main
            SET status = :TargetStatus,
                updated_at = :UpdatedAt,
                pay_amount = CASE WHEN :IsCancelled = 1 THEN 0 ELSE pay_amount END
            WHERE id = :OrderId AND status = :ExpectedStatus";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("TargetStatus", targetStatus));
        cmd.Parameters.Add(CreateParameter("UpdatedAt", updatedAt));
        cmd.Parameters.Add(CreateParameter("IsCancelled", targetStatus == 4 ? 1 : 0));
        cmd.Parameters.Add(CreateParameter("OrderId", orderId));
        cmd.Parameters.Add(CreateParameter("ExpectedStatus", expectedStatus));

        return await cmd.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    // 查询单条
    public async Task<OrderMain?> GetOrderByIdAsync(long orderId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM order_main WHERE id = :Id";
        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("Id", orderId));

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapOrderMain(reader);
        }
        return null;
    }

    public async Task<OrderMain?> GetFullOrderAsync(long orderId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var order = await GetOrderByIdAsync(orderId, cancellationToken);
        if (order == null) return null;

        // 加载明细
        const string itemSql = "SELECT * FROM order_item WHERE order_id = :OrderId";
        await using var cmdItem = Connection.CreateCommand();
        cmdItem.CommandText = itemSql;
        cmdItem.Transaction = Transaction;
        cmdItem.Parameters.Add(CreateParameter("OrderId", orderId));

        var items = new List<OrderItem>();
        await using var readerItem = await cmdItem.ExecuteReaderAsync(cancellationToken);
        while (await readerItem.ReadAsync(cancellationToken))
        {
            items.Add(MapOrderItem(readerItem));
        }
        order.Items = items;

        // 加载日志
        const string logSql = "SELECT * FROM order_log WHERE order_id = :OrderId ORDER BY created_at";
        await using var cmdLog = Connection.CreateCommand();
        cmdLog.CommandText = logSql;
        cmdLog.Transaction = Transaction;
        cmdLog.Parameters.Add(CreateParameter("OrderId", orderId));

        var logs = new List<OrderLog>();
        await using var readerLog = await cmdLog.ExecuteReaderAsync(cancellationToken);
        while (await readerLog.ReadAsync(cancellationToken))
        {
            logs.Add(MapOrderLog(readerLog));
        }
        order.Logs = logs;

        return order;
    }

    // 辅助查询
    public async Task<IReadOnlyList<OrderSkuQuantity>> GetOrderSkuQuantitiesAsync(long orderId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT sku_id AS SkuId, quantity FROM order_item WHERE order_id = :OrderId";
        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("OrderId", orderId));

        var result = new List<OrderSkuQuantity>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new OrderSkuQuantity(
                reader.GetInt64(reader.GetOrdinal("SkuId")),
                reader.GetInt32(reader.GetOrdinal("Quantity"))
            ));
        }
        return result;
    }

    public async Task<OrderPaymentContextDto?> GetPaymentContextAsync(long orderId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT 
                id AS OrderId,
                order_no AS OrderNo,
                user_id AS UserId,
                status AS Status,
                pay_amount AS PayAmount,
                user_coupon_id AS UserCouponId,
                pay_expire_time AS PayExpireTime
            FROM order_main
            WHERE id = :OrderId";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("OrderId", orderId));

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new OrderPaymentContextDto(
                reader.GetInt64(reader.GetOrdinal("OrderId")),
                reader.GetString(reader.GetOrdinal("OrderNo")),
                reader.GetInt64(reader.GetOrdinal("UserId")),
                reader.GetInt32(reader.GetOrdinal("Status")),
                reader.GetDecimal(reader.GetOrdinal("PayAmount")),
                reader.IsDBNull(reader.GetOrdinal("UserCouponId")) ? null : reader.GetInt64(reader.GetOrdinal("UserCouponId")),
                reader.GetDateTime(reader.GetOrdinal("PayExpireTime"))
            );
        }
        return null;
    }

    // 定时任务
    public async Task<IReadOnlyList<long>> GetExpiredOrderIdsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT id FROM order_main WHERE status = 0 AND pay_expire_time < :CutoffTime";
        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("CutoffTime", cutoffTime));

        var result = new List<long>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(reader.GetInt64(0));
        }
        return result;
    }

    // 分页查询
    public async Task<PagedResult<OrderListItemDto>> SearchUserOrdersAsync(long userId, OrderQuery query, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var where = new StringBuilder("WHERE user_id = :UserId");

        if (query.Status.HasValue)
        {
            where.Append(" AND status = :Status");
        }
        if (query.StartTime.HasValue)
        {
            where.Append(" AND created_at >= :StartTime");
        }
        if (query.EndTime.HasValue)
        {
            where.Append(" AND created_at < :EndTime");
        }

        // 总条数
        var countSql = $"SELECT COUNT(*) FROM order_main {where}";
        await using var cmdCount = Connection.CreateCommand();
        cmdCount.CommandText = countSql;
        cmdCount.Transaction = Transaction;
        AddUserOrderQueryParameters(cmdCount, userId, query);
        var totalCount = Convert.ToInt64(await cmdCount.ExecuteScalarAsync(cancellationToken));

        if (totalCount == 0)
            return PagedResult<OrderListItemDto>.Empty(query.SafePageIndex, query.SafePageSize);

        // 数据
        var offset = (query.SafePageIndex - 1) * query.SafePageSize;
        var dataSql = $@"
            SELECT 
                id AS OrderId,
                order_no AS OrderNo,
                user_id AS UserId,
                status AS Status,
                pay_amount AS PayAmount,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM order_main
            {where}
            ORDER BY created_at DESC
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY";

        await using var cmdData = Connection.CreateCommand();
        cmdData.CommandText = dataSql;
        cmdData.Transaction = Transaction;
        AddUserOrderQueryParameters(cmdData, userId, query);
        cmdData.Parameters.Add(CreateParameter("Offset", offset));
        cmdData.Parameters.Add(CreateParameter("PageSize", query.SafePageSize));

        var items = new List<OrderListItemDto>();
        await using var reader = await cmdData.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new OrderListItemDto(
                reader.GetInt64(reader.GetOrdinal("OrderId")),
                reader.GetString(reader.GetOrdinal("OrderNo")),
                reader.GetInt64(reader.GetOrdinal("UserId")),
                reader.GetInt32(reader.GetOrdinal("Status")),
                reader.GetDecimal(reader.GetOrdinal("PayAmount")),
                reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            ));
        }

        return new PagedResult<OrderListItemDto>(items, query.SafePageIndex, query.SafePageSize, totalCount);
    }

    public async Task<PagedResult<OrderListItemDto>> SearchAdminOrdersAsync(AdminOrderQuery query, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var where = new StringBuilder("WHERE 1=1");

        if (query.UserId.HasValue)
        {
            where.Append(" AND user_id = :UserId");
        }
        if (!string.IsNullOrWhiteSpace(query.OrderNo))
        {
            where.Append(" AND order_no = :OrderNo");
        }
        if (query.Status.HasValue)
        {
            where.Append(" AND status = :Status");
        }
        if (query.StartTime.HasValue)
        {
            where.Append(" AND created_at >= :StartTime");
        }
        if (query.EndTime.HasValue)
        {
            where.Append(" AND created_at < :EndTime");
        }

        var countSql = $"SELECT COUNT(*) FROM order_main {where}";
        await using var cmdCount = Connection.CreateCommand();
        cmdCount.CommandText = countSql;
        cmdCount.Transaction = Transaction;
        AddAdminOrderQueryParameters(cmdCount, query);
        var totalCount = Convert.ToInt64(await cmdCount.ExecuteScalarAsync(cancellationToken));

        if (totalCount == 0)
            return PagedResult<OrderListItemDto>.Empty(query.SafePageIndex, query.SafePageSize);

        var offset = (query.SafePageIndex - 1) * query.SafePageSize;
        var dataSql = $@"
            SELECT 
                id AS OrderId,
                order_no AS OrderNo,
                user_id AS UserId,
                status AS Status,
                pay_amount AS PayAmount,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM order_main
            {where}
            ORDER BY created_at DESC
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY";

        await using var cmdData = Connection.CreateCommand();
        cmdData.CommandText = dataSql;
        cmdData.Transaction = Transaction;
        AddAdminOrderQueryParameters(cmdData, query);
        cmdData.Parameters.Add(CreateParameter("Offset", offset));
        cmdData.Parameters.Add(CreateParameter("PageSize", query.SafePageSize));

        var items = new List<OrderListItemDto>();
        await using var reader = await cmdData.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new OrderListItemDto(
                reader.GetInt64(reader.GetOrdinal("OrderId")),
                reader.GetString(reader.GetOrdinal("OrderNo")),
                reader.GetInt64(reader.GetOrdinal("UserId")),
                reader.GetInt32(reader.GetOrdinal("Status")),
                reader.GetDecimal(reader.GetOrdinal("PayAmount")),
                reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            ));
        }

        return new PagedResult<OrderListItemDto>(items, query.SafePageIndex, query.SafePageSize, totalCount);
    }

    private static void AddUserOrderQueryParameters(DbCommand command, long userId, OrderQuery query)
    {
        command.Parameters.Add(CreateParameter("UserId", userId));

        if (query.Status.HasValue)
        {
            command.Parameters.Add(CreateParameter("Status", query.Status.Value));
        }
        if (query.StartTime.HasValue)
        {
            command.Parameters.Add(CreateParameter("StartTime", query.StartTime.Value));
        }
        if (query.EndTime.HasValue)
        {
            command.Parameters.Add(CreateParameter("EndTime", query.EndTime.Value.AddDays(1)));
        }
    }

    private static void AddAdminOrderQueryParameters(DbCommand command, AdminOrderQuery query)
    {
        if (query.UserId.HasValue)
        {
            command.Parameters.Add(CreateParameter("UserId", query.UserId.Value));
        }
        if (!string.IsNullOrWhiteSpace(query.OrderNo))
        {
            command.Parameters.Add(CreateParameter("OrderNo", query.OrderNo.Trim()));
        }
        if (query.Status.HasValue)
        {
            command.Parameters.Add(CreateParameter("Status", query.Status.Value));
        }
        if (query.StartTime.HasValue)
        {
            command.Parameters.Add(CreateParameter("StartTime", query.StartTime.Value));
        }
        if (query.EndTime.HasValue)
        {
            command.Parameters.Add(CreateParameter("EndTime", query.EndTime.Value.AddDays(1)));
        }
    }

    // 映射辅助
    private static OrderMain MapOrderMain(DbDataReader reader)
    {
        return new OrderMain
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            OrderNo = reader.GetString(reader.GetOrdinal("order_no")),
            UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
            AddressId = reader.GetInt64(reader.GetOrdinal("address_id")),
            UserCouponId = reader.IsDBNull(reader.GetOrdinal("user_coupon_id")) ? null : reader.GetInt64(reader.GetOrdinal("user_coupon_id")),
            Status = reader.GetInt32(reader.GetOrdinal("status")),
            TotalAmount = reader.GetDecimal(reader.GetOrdinal("total_amount")),
            DiscountAmount = reader.GetDecimal(reader.GetOrdinal("discount_amount")),
            PayAmount = reader.GetDecimal(reader.GetOrdinal("pay_amount")),
            PayExpireTime = reader.GetDateTime(reader.GetOrdinal("pay_expire_time")),
            ReceiverSnapshot = reader.GetString(reader.GetOrdinal("receiver_snapshot")),
            Remark = reader.IsDBNull(reader.GetOrdinal("remark")) ? null : reader.GetString(reader.GetOrdinal("remark")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }

    private static OrderItem MapOrderItem(DbDataReader reader)
    {
        return new OrderItem
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            OrderId = reader.GetInt64(reader.GetOrdinal("order_id")),
            SkuId = reader.GetInt64(reader.GetOrdinal("sku_id")),
            ProductNameSnap = reader.GetString(reader.GetOrdinal("product_name_snap")),
            SpecSnap = reader.GetString(reader.GetOrdinal("spec_snap")),
            MainImageSnap = reader.GetString(reader.GetOrdinal("main_image_snap")),
            UnitPrice = reader.GetDecimal(reader.GetOrdinal("unit_price")),
            Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
            Subtotal = reader.GetDecimal(reader.GetOrdinal("subtotal"))
        };
    }

    private static OrderLog MapOrderLog(DbDataReader reader)
    {
        return new OrderLog
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            OrderId = reader.GetInt64(reader.GetOrdinal("order_id")),
            FromStatus = reader.IsDBNull(reader.GetOrdinal("from_status")) ? null : reader.GetInt32(reader.GetOrdinal("from_status")),
            ToStatus = reader.GetInt32(reader.GetOrdinal("to_status")),
            OperatorId = reader.IsDBNull(reader.GetOrdinal("operator_id")) ? null : reader.GetInt64(reader.GetOrdinal("operator_id")),
            OperatorName = reader.IsDBNull(reader.GetOrdinal("operator_name")) ? null : reader.GetString(reader.GetOrdinal("operator_name")),
            Remark = reader.IsDBNull(reader.GetOrdinal("remark")) ? null : reader.GetString(reader.GetOrdinal("remark")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }

    private static DbParameter CreateParameter(string name, object? value)
    {
        return new OracleParameter(name, value ?? DBNull.Value);
    }
}
