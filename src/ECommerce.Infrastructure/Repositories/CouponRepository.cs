// SQL语句，实现分页查询、根据ID查询、新增、修改信息、修改启停状态这5个数据库操作
using ECommerce.Domain.Entities;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;
using System.Text;

namespace ECommerce.Infrastructure.Repositories;

public class CouponRepository : ICouponRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public CouponRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private DbConnection Connection => _unitOfWork.CurrentConnection ?? throw new InvalidOperationException("Connection not opened. Call GetOpenConnectionAsync first.");
    private DbTransaction? Transaction => _unitOfWork.CurrentTransaction;

    public async Task<PagedResult<CouponTemplate>> GetTemplatesAsync(string? keyword, int? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        
        var whereBuilder = new StringBuilder("WHERE 1=1");
        var parameters = new List<DbParameter>();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            whereBuilder.Append(" AND name LIKE :Keyword");
            parameters.Add(CreateParameter("Keyword", $"%{keyword}%"));
        }

        if (status.HasValue)
        {
            whereBuilder.Append(" AND status = :Status");
            parameters.Add(CreateParameter("Status", status.Value));
        }

        string where = whereBuilder.ToString();

        // 查总数
        string countSql = $"SELECT COUNT(1) FROM coupon_template {where}";
        await using var cmdCount = Connection.CreateCommand();
        cmdCount.CommandText = countSql;
        cmdCount.Transaction = Transaction;
        foreach (var p in parameters) cmdCount.Parameters.Add(p);

        long totalCount = Convert.ToInt64(await cmdCount.ExecuteScalarAsync(cancellationToken));
        if (totalCount == 0)
        {
            return PagedResult<CouponTemplate>.Empty(pageIndex, pageSize);
        }

        // 分页查询
        int offset = (pageIndex - 1) * pageSize;
        string dataSql = $@"
            SELECT id, name, type, amount, min_amount, total_count, received_count, start_time, end_time, status
            FROM coupon_template 
            {where}
            ORDER BY id DESC
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY";

        var dataParams = new List<DbParameter>(parameters);
        dataParams.Add(CreateParameter("Offset", offset));
        dataParams.Add(CreateParameter("PageSize", pageSize));

        await using var cmdData = Connection.CreateCommand();
        cmdData.CommandText = dataSql;
        cmdData.Transaction = Transaction;
        foreach (var p in dataParams) cmdData.Parameters.Add(p);

        var items = new List<CouponTemplate>();
        await using var reader = await cmdData.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapCouponTemplate(reader));
        }

        return new PagedResult<CouponTemplate>(items, pageIndex, pageSize, totalCount);
    }

    public async Task<CouponTemplate?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        string sql = @"
            SELECT id, name, type, amount, min_amount, total_count, received_count, start_time, end_time, status
            FROM coupon_template 
            WHERE id = :Id";
            
        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("Id", id));

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapCouponTemplate(reader);
        }
        return null;
    }

    public async Task<int> InsertTemplateAsync(CouponTemplate template, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO coupon_template 
                (name, type, amount, min_amount, total_count, received_count, start_time, end_time, status)
            VALUES 
                (:Name, :Type, :Amount, :MinAmount, :TotalCount, :ReceivedCount, :StartTime, :EndTime, :Status)
            RETURNING id INTO :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        cmd.Parameters.Add(CreateParameter("Name", template.Name));
        cmd.Parameters.Add(CreateParameter("Type", template.Type));
        cmd.Parameters.Add(CreateParameter("Amount", template.Amount));
        cmd.Parameters.Add(CreateParameter("MinAmount", template.MinAmount));
        cmd.Parameters.Add(CreateParameter("TotalCount", template.TotalCount));
        cmd.Parameters.Add(CreateParameter("ReceivedCount", template.ReceivedCount));
        cmd.Parameters.Add(CreateParameter("StartTime", template.StartTime));
        cmd.Parameters.Add(CreateParameter("EndTime", template.EndTime));
        cmd.Parameters.Add(CreateParameter("Status", template.Status));

        var pId = cmd.CreateParameter();
        pId.ParameterName = "Id";
        pId.DbType = DbType.Int32;
        pId.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(pId);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        template.Id = Convert.ToInt32(pId.Value);
        return template.Id;
    }

    public async Task<bool> UpdateTemplateAsync(CouponTemplate template, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE coupon_template 
            SET name = :Name, type = :Type, amount = :Amount, min_amount = :MinAmount, 
                total_count = :TotalCount, start_time = :StartTime, end_time = :EndTime, status = :Status
            WHERE id = :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        cmd.Parameters.Add(CreateParameter("Name", template.Name));
        cmd.Parameters.Add(CreateParameter("Type", template.Type));
        cmd.Parameters.Add(CreateParameter("Amount", template.Amount));
        cmd.Parameters.Add(CreateParameter("MinAmount", template.MinAmount));
        cmd.Parameters.Add(CreateParameter("TotalCount", template.TotalCount));
        cmd.Parameters.Add(CreateParameter("StartTime", template.StartTime));
        cmd.Parameters.Add(CreateParameter("EndTime", template.EndTime));
        cmd.Parameters.Add(CreateParameter("Status", template.Status));
        cmd.Parameters.Add(CreateParameter("Id", template.Id));

        return await cmd.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> UpdateTemplateStatusAsync(int id, int status, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"UPDATE coupon_template SET status = :Status WHERE id = :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        cmd.Parameters.Add(CreateParameter("Status", status));
        cmd.Parameters.Add(CreateParameter("Id", id));

        return await cmd.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static CouponTemplate MapCouponTemplate(DbDataReader reader)
    {
        return new CouponTemplate
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Type = reader.GetInt32(reader.GetOrdinal("type")),
            Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
            MinAmount = reader.GetDecimal(reader.GetOrdinal("min_amount")),
            TotalCount = reader.GetInt32(reader.GetOrdinal("total_count")),
            ReceivedCount = reader.GetInt32(reader.GetOrdinal("received_count")),
            StartTime = reader.GetDateTime(reader.GetOrdinal("start_time")),
            EndTime = reader.GetDateTime(reader.GetOrdinal("end_time")),
            Status = reader.GetInt32(reader.GetOrdinal("status"))
        };
    }

    private static DbParameter CreateParameter(string name, object? value)
    {
        return new OracleParameter(name, value ?? DBNull.Value);
    }

    // --- User Coupon Methods ---

    public async Task<long> InsertUserCouponAsync(UserCoupon userCoupon, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO user_coupon 
                (user_id, coupon_template_id, status)
            VALUES 
                (:UserId, :TemplateId, :Status)
            RETURNING id INTO :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        cmd.Parameters.Add(CreateParameter("UserId", userCoupon.UserId));
        cmd.Parameters.Add(CreateParameter("TemplateId", userCoupon.CouponTemplateId));
        cmd.Parameters.Add(CreateParameter("Status", userCoupon.Status));

        var pId = cmd.CreateParameter();
        pId.ParameterName = "Id";
        pId.DbType = DbType.Int64;
        pId.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(pId);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        userCoupon.Id = Convert.ToInt64(pId.Value);
        return userCoupon.Id;
    }

    public async Task<bool> IncrementTemplateReceivedCountAsync(int templateId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        // Ensure we only increment if we haven't reached total_count, or if total_count == -1
        const string sql = @"
            UPDATE coupon_template 
            SET received_count = received_count + 1 
            WHERE id = :Id AND (total_count = -1 OR received_count < total_count)";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("Id", templateId));

        return await cmd.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<IReadOnlyList<UserCoupon>> GetUserCouponsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT uc.id, uc.user_id, uc.coupon_template_id, uc.status, uc.received_at, uc.used_at, uc.order_id,
                   ct.name AS coupon_name
            FROM user_coupon uc
            JOIN coupon_template ct ON uc.coupon_template_id = ct.id
            WHERE uc.user_id = :UserId
            ORDER BY uc.received_at DESC";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("UserId", userId));

        var items = new List<UserCoupon>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new UserCoupon
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
                CouponTemplateId = reader.GetInt32(reader.GetOrdinal("coupon_template_id")),
                Status = reader.GetInt32(reader.GetOrdinal("status")),
                ReceivedAt = reader.GetDateTime(reader.GetOrdinal("received_at")),
                UsedAt = reader.IsDBNull(reader.GetOrdinal("used_at")) ? null : reader.GetDateTime(reader.GetOrdinal("used_at")),
                OrderId = reader.IsDBNull(reader.GetOrdinal("order_id")) ? null : reader.GetInt64(reader.GetOrdinal("order_id")),
                CouponName = reader.GetString(reader.GetOrdinal("coupon_name"))
            });
        }
        return items;
    }

    public async Task<UserCoupon?> GetUserCouponByIdAsync(long userCouponId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, user_id, coupon_template_id, status, received_at, used_at, order_id
            FROM user_coupon 
            WHERE id = :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("Id", userCouponId));

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new UserCoupon
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
                CouponTemplateId = reader.GetInt32(reader.GetOrdinal("coupon_template_id")),
                Status = reader.GetInt32(reader.GetOrdinal("status")),
                ReceivedAt = reader.GetDateTime(reader.GetOrdinal("received_at")),
                UsedAt = reader.IsDBNull(reader.GetOrdinal("used_at")) ? null : reader.GetDateTime(reader.GetOrdinal("used_at")),
                OrderId = reader.IsDBNull(reader.GetOrdinal("order_id")) ? null : reader.GetInt64(reader.GetOrdinal("order_id")),
            };
        }
        return null;
    }

    public async Task<bool> UpdateUserCouponStatusAsync(long userCouponId, int status, long? orderId, DateTime? usedAt, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE user_coupon 
            SET status = :Status, order_id = :OrderId, used_at = :UsedAt
            WHERE id = :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        cmd.Parameters.Add(CreateParameter("Status", status));
        cmd.Parameters.Add(CreateParameter("OrderId", orderId));
        cmd.Parameters.Add(CreateParameter("UsedAt", usedAt));
        cmd.Parameters.Add(CreateParameter("Id", userCouponId));

        return await cmd.ExecuteNonQueryAsync(cancellationToken) > 0;
    }
}
