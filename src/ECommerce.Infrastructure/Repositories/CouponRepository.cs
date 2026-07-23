using System.Data;
using System.Data.Common;
using System.Text;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Data;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Infrastructure.Repositories;

public sealed class CouponRepository : ICouponRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public CouponRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private DbConnection Connection => _unitOfWork.CurrentConnection
        ?? throw new InvalidOperationException("Connection not opened. Call GetOpenConnectionAsync first.");

    private DbTransaction? Transaction => _unitOfWork.CurrentTransaction;

    public async Task<PagedResult<CouponTemplate>> GetTemplatesAsync(
        string? keyword,
        int? status,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var where = new StringBuilder("WHERE 1 = 1");
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            where.Append(" AND ct.name LIKE :Keyword");
        }

        if (status.HasValue)
        {
            where.Append(" AND ct.status = :Status");
        }

        await using var countCommand = CreateCommand($"SELECT COUNT(1) FROM coupon_template ct {where}");
        AddTemplateQueryParameters(countCommand, keyword, status);
        var totalCount = Convert.ToInt64(await countCommand.ExecuteScalarAsync(cancellationToken));
        if (totalCount == 0)
        {
            return PagedResult<CouponTemplate>.Empty(pageIndex, pageSize);
        }

        var sql = $@"
            SELECT ct.id, ct.name, ct.type, ct.amount, ct.min_amount, ct.total_count,
                   ct.received_count, ct.start_time, ct.end_time, ct.status,
                   ct.applicable_category_id, c.name AS applicable_category_name
            FROM coupon_template ct
            LEFT JOIN category c ON c.id = ct.applicable_category_id
            {where}
            ORDER BY ct.id DESC
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY";
        await using var dataCommand = CreateCommand(sql);
        AddTemplateQueryParameters(dataCommand, keyword, status);
        dataCommand.Parameters.Add(CreateParameter("Offset", (pageIndex - 1) * pageSize));
        dataCommand.Parameters.Add(CreateParameter("PageSize", pageSize));

        var items = new List<CouponTemplate>();
        await using var reader = await dataCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapCouponTemplate(reader));
        }

        return new PagedResult<CouponTemplate>(items, pageIndex, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<CouponTemplate>> GetAvailableTemplatesAsync(
        long userId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT ct.id, ct.name, ct.type, ct.amount, ct.min_amount, ct.total_count,
                   ct.received_count, ct.start_time, ct.end_time, ct.status,
                   ct.applicable_category_id, c.name AS applicable_category_name
            FROM coupon_template ct
            LEFT JOIN category c ON c.id = ct.applicable_category_id
            WHERE ct.status = 1
              AND ct.start_time <= :Now
              AND ct.end_time >= :Now
              AND (ct.total_count = -1 OR ct.received_count < ct.total_count)
              AND NOT EXISTS (
                  SELECT 1
                  FROM user_coupon uc
                  WHERE uc.user_id = :UserId
                    AND uc.coupon_template_id = ct.id)
            ORDER BY ct.end_time, ct.id";

        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("Now", now));
        command.Parameters.Add(CreateParameter("UserId", userId));
        var items = new List<CouponTemplate>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapCouponTemplate(reader));
        }

        return items;
    }

    public async Task<CouponTemplate?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT ct.id, ct.name, ct.type, ct.amount, ct.min_amount, ct.total_count,
                   ct.received_count, ct.start_time, ct.end_time, ct.status,
                   ct.applicable_category_id, c.name AS applicable_category_name
            FROM coupon_template ct
            LEFT JOIN category c ON c.id = ct.applicable_category_id
            WHERE ct.id = :Id";
        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("Id", id));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapCouponTemplate(reader) : null;
    }

    public async Task<int> InsertTemplateAsync(CouponTemplate template, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO coupon_template
                (name, type, amount, min_amount, total_count, received_count, start_time, end_time, status, applicable_category_id)
            VALUES
                (:Name, :Type, :Amount, :MinAmount, :TotalCount, :ReceivedCount, :StartTime, :EndTime, :Status, :ApplicableCategoryId)
            RETURNING id INTO :Id";
        await using var command = CreateCommand(sql);
        AddTemplateParameters(command, template, includeReceivedCount: true);
        var idParameter = command.CreateParameter();
        idParameter.ParameterName = "Id";
        idParameter.DbType = DbType.Int32;
        idParameter.Direction = ParameterDirection.Output;
        command.Parameters.Add(idParameter);
        await command.ExecuteNonQueryAsync(cancellationToken);
        template.Id = OracleValueConverter.ToInt32(idParameter.Value);
        return template.Id;
    }

    public async Task<bool> UpdateTemplateAsync(CouponTemplate template, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE coupon_template
            SET name = :Name,
                type = :Type,
                amount = :Amount,
                min_amount = :MinAmount,
                total_count = :TotalCount,
                start_time = :StartTime,
                end_time = :EndTime,
                status = :Status,
                applicable_category_id = :ApplicableCategoryId
            WHERE id = :Id";
        await using var command = CreateCommand(sql);
        AddTemplateParameters(command, template, includeReceivedCount: false);
        command.Parameters.Add(CreateParameter("Id", template.Id));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> UpdateTemplateStatusAsync(int id, int status, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand("UPDATE coupon_template SET status = :Status WHERE id = :Id");
        command.Parameters.Add(CreateParameter("Status", status));
        command.Parameters.Add(CreateParameter("Id", id));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> IsEnabledLeafCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT COUNT(1)
            FROM category c
            WHERE c.id = :CategoryId
              AND c.status = 1
              AND NOT EXISTS (
                  SELECT 1
                  FROM category child
                  WHERE child.parent_id = c.id)";
        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("CategoryId", categoryId));
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    public async Task<bool> TryIncrementReceivedCountAsync(
        int templateId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE coupon_template
            SET received_count = received_count + 1
            WHERE id = :TemplateId
              AND status = 1
              AND start_time <= :Now
              AND end_time >= :Now
              AND (total_count = -1 OR received_count < total_count)";
        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("TemplateId", templateId));
        command.Parameters.Add(CreateParameter("Now", now));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<long> InsertUserCouponAsync(UserCoupon userCoupon, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO user_coupon (user_id, coupon_template_id, status, received_at, used_at, order_id)
            VALUES (:UserId, :TemplateId, :Status, :ReceivedAt, :UsedAt, :OrderId)
            RETURNING id INTO :Id";
        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("UserId", userCoupon.UserId));
        command.Parameters.Add(CreateParameter("TemplateId", userCoupon.CouponTemplateId));
        command.Parameters.Add(CreateParameter("Status", userCoupon.Status));
        command.Parameters.Add(CreateParameter("ReceivedAt", userCoupon.ReceivedAt));
        command.Parameters.Add(CreateParameter("UsedAt", userCoupon.UsedAt));
        command.Parameters.Add(CreateParameter("OrderId", userCoupon.OrderId));
        var idParameter = command.CreateParameter();
        idParameter.ParameterName = "Id";
        idParameter.DbType = DbType.Int64;
        idParameter.Direction = ParameterDirection.Output;
        command.Parameters.Add(idParameter);
        await command.ExecuteNonQueryAsync(cancellationToken);
        userCoupon.Id = OracleValueConverter.ToInt64(idParameter.Value);
        return userCoupon.Id;
    }

    public async Task<IReadOnlyList<UserCouponWithTemplate>> GetUserCouponsAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT uc.id AS uc_id, uc.user_id, uc.coupon_template_id, uc.status AS uc_status,
                   uc.received_at, uc.used_at, uc.order_id,
                   ct.id, ct.name, ct.type, ct.amount, ct.min_amount, ct.total_count,
                   ct.received_count, ct.start_time, ct.end_time, ct.status,
                   ct.applicable_category_id, c.name AS applicable_category_name
            FROM user_coupon uc
            INNER JOIN coupon_template ct ON ct.id = uc.coupon_template_id
            LEFT JOIN category c ON c.id = ct.applicable_category_id
            WHERE uc.user_id = :UserId
            ORDER BY uc.status, uc.received_at DESC";
        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("UserId", userId));
        var items = new List<UserCouponWithTemplate>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapUserCouponWithTemplate(reader));
        }

        return items;
    }

    public async Task<UserCouponWithTemplate?> GetUserCouponAsync(
        long userId,
        long userCouponId,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT uc.id AS uc_id, uc.user_id, uc.coupon_template_id, uc.status AS uc_status,
                   uc.received_at, uc.used_at, uc.order_id,
                   ct.id, ct.name, ct.type, ct.amount, ct.min_amount, ct.total_count,
                   ct.received_count, ct.start_time, ct.end_time, ct.status,
                   ct.applicable_category_id, c.name AS applicable_category_name
            FROM user_coupon uc
            INNER JOIN coupon_template ct ON ct.id = uc.coupon_template_id
            LEFT JOIN category c ON c.id = ct.applicable_category_id
            WHERE uc.id = :UserCouponId
              AND uc.user_id = :UserId";
        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("UserCouponId", userCouponId));
        command.Parameters.Add(CreateParameter("UserId", userId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapUserCouponWithTemplate(reader) : null;
    }

    public async Task<bool> TryUseForOrderAsync(
        long userId,
        long userCouponId,
        long orderId,
        decimal eligibleAmount,
        decimal expectedDiscountAmount,
        DateTime usedAt,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE user_coupon
            SET status = :UsedStatus,
                used_at = :UsedAt,
                order_id = :OrderId
            WHERE id = :UserCouponId
              AND user_id = :UserId
              AND status = :UnusedStatus
              AND EXISTS (
                  SELECT 1
                  FROM coupon_template ct
                  WHERE ct.id = user_coupon.coupon_template_id
                    AND ct.status = 1
                    AND ct.start_time <= :UsedAt
                    AND ct.end_time >= :UsedAt
                    AND ct.min_amount <= :EligibleAmount
                    AND ((ct.type = 1 AND ct.amount > 0 AND ct.amount <= :EligibleAmount
                          AND ct.amount = :ExpectedDiscountAmount)
                      OR (ct.type = 2 AND ct.amount > 0 AND ct.amount <= 1
                          AND ROUND(:EligibleAmount * (1 - ct.amount), 2) = :ExpectedDiscountAmount)))";
        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("UsedStatus", (int)UserCouponStatus.Used));
        command.Parameters.Add(CreateParameter("UsedAt", usedAt));
        command.Parameters.Add(CreateParameter("OrderId", orderId));
        command.Parameters.Add(CreateParameter("UserCouponId", userCouponId));
        command.Parameters.Add(CreateParameter("UserId", userId));
        command.Parameters.Add(CreateParameter("UnusedStatus", (int)UserCouponStatus.Unused));
        command.Parameters.Add(CreateParameter("EligibleAmount", eligibleAmount));
        command.Parameters.Add(CreateParameter("ExpectedDiscountAmount", expectedDiscountAmount));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> TryRestoreForOrderAsync(
        long userId,
        long userCouponId,
        long orderId,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE user_coupon
            SET status = :UnusedStatus,
                used_at = NULL,
                order_id = NULL
            WHERE id = :UserCouponId
              AND user_id = :UserId
              AND status = :UsedStatus
              AND order_id = :OrderId";
        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("UnusedStatus", (int)UserCouponStatus.Unused));
        command.Parameters.Add(CreateParameter("UserCouponId", userCouponId));
        command.Parameters.Add(CreateParameter("UserId", userId));
        command.Parameters.Add(CreateParameter("UsedStatus", (int)UserCouponStatus.Used));
        command.Parameters.Add(CreateParameter("OrderId", orderId));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private DbCommand CreateCommand(string sql)
    {
        var command = Connection.CreateCommand();
        if (command is OracleCommand oracleCommand)
        {
            oracleCommand.BindByName = true;
        }

        command.CommandText = sql;
        command.Transaction = Transaction;
        return command;
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
            Status = reader.GetInt32(reader.GetOrdinal("status")),
            ApplicableCategoryId = reader.IsDBNull(reader.GetOrdinal("applicable_category_id"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("applicable_category_id")),
            ApplicableCategoryName = reader.IsDBNull(reader.GetOrdinal("applicable_category_name"))
                ? null
                : reader.GetString(reader.GetOrdinal("applicable_category_name"))
        };
    }

    private static UserCouponWithTemplate MapUserCouponWithTemplate(DbDataReader reader)
    {
        var userCoupon = new UserCoupon
        {
            Id = reader.GetInt64(reader.GetOrdinal("uc_id")),
            UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
            CouponTemplateId = reader.GetInt32(reader.GetOrdinal("coupon_template_id")),
            Status = reader.GetInt32(reader.GetOrdinal("uc_status")),
            ReceivedAt = reader.GetDateTime(reader.GetOrdinal("received_at")),
            UsedAt = reader.IsDBNull(reader.GetOrdinal("used_at")) ? null : reader.GetDateTime(reader.GetOrdinal("used_at")),
            OrderId = reader.IsDBNull(reader.GetOrdinal("order_id")) ? null : reader.GetInt64(reader.GetOrdinal("order_id"))
        };
        return new UserCouponWithTemplate(userCoupon, MapCouponTemplate(reader));
    }

    private static void AddTemplateQueryParameters(DbCommand command, string? keyword, int? status)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            command.Parameters.Add(CreateParameter("Keyword", $"%{keyword.Trim()}%"));
        }

        if (status.HasValue)
        {
            command.Parameters.Add(CreateParameter("Status", status.Value));
        }
    }

    private static void AddTemplateParameters(DbCommand command, CouponTemplate template, bool includeReceivedCount)
    {
        command.Parameters.Add(CreateParameter("Name", template.Name));
        command.Parameters.Add(CreateParameter("Type", template.Type));
        command.Parameters.Add(CreateParameter("Amount", template.Amount));
        command.Parameters.Add(CreateParameter("MinAmount", template.MinAmount));
        command.Parameters.Add(CreateParameter("TotalCount", template.TotalCount));
        if (includeReceivedCount)
        {
            command.Parameters.Add(CreateParameter("ReceivedCount", template.ReceivedCount));
        }

        command.Parameters.Add(CreateParameter("StartTime", template.StartTime));
        command.Parameters.Add(CreateParameter("EndTime", template.EndTime));
        command.Parameters.Add(CreateParameter("Status", template.Status));
        command.Parameters.Add(CreateParameter("ApplicableCategoryId", template.ApplicableCategoryId));
    }

    private static DbParameter CreateParameter(string name, object? value)
    {
        return new OracleParameter(name, value ?? DBNull.Value);
    }
}
