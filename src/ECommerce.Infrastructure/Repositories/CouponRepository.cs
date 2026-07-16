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

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            whereBuilder.Append(" AND name LIKE :Keyword");
        }

        if (status.HasValue)
        {
            whereBuilder.Append(" AND status = :Status");
        }

        string where = whereBuilder.ToString();

        // 查总数
        string countSql = $"SELECT COUNT(1) FROM coupon_template {where}";
        await using var cmdCount = Connection.CreateCommand();
        cmdCount.CommandText = countSql;
        cmdCount.Transaction = Transaction;
        AddTemplateQueryParameters(cmdCount, keyword, status);

        long totalCount = Convert.ToInt64(await cmdCount.ExecuteScalarAsync(cancellationToken));
        if (totalCount == 0)
        {
            return PagedResult<CouponTemplate>.Empty(pageIndex, pageSize);
        }

        // 分页查询
        int offset = (pageIndex - 1) * pageSize;
        string dataSql = $@"
            SELECT id,
                   name,
                   type,
                   amount AS face_value,
                   min_amount AS min_consumption,
                   total_count AS total_issue,
                   received_count AS issued_count,
                   start_time AS valid_start_time,
                   end_time AS valid_end_time,
                   status
            FROM coupon_template 
            {where}
            ORDER BY id DESC
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY";

        await using var cmdData = Connection.CreateCommand();
        cmdData.CommandText = dataSql;
        cmdData.Transaction = Transaction;
        AddTemplateQueryParameters(cmdData, keyword, status);
        cmdData.Parameters.Add(CreateParameter("Offset", offset));
        cmdData.Parameters.Add(CreateParameter("PageSize", pageSize));

        var items = new List<CouponTemplate>();
        await using var reader = await cmdData.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapCouponTemplate(reader));
        }

        return new PagedResult<CouponTemplate>(items, pageIndex, pageSize, totalCount);
    }
    
    // 根据 ID 查询单条
    public async Task<CouponTemplate?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        string sql = @"
            SELECT id,
                   name,
                   type,
                   amount AS face_value,
                   min_amount AS min_consumption,
                   total_count AS total_issue,
                   received_count AS issued_count,
                   start_time AS valid_start_time,
                   end_time AS valid_end_time,
                   status
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
    
    // 新增优惠券模板
    public async Task<int> InsertTemplateAsync(CouponTemplate template, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO coupon_template 
                (name, type, amount, min_amount, total_count, received_count, start_time, end_time, status)
            VALUES 
                (:Name, :Type, :FaceValue, :MinConsumption, :TotalIssue, :IssuedCount, :ValidStartTime, :ValidEndTime, :Status)
            RETURNING id INTO :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        cmd.Parameters.Add(CreateParameter("Name", template.Name));
        cmd.Parameters.Add(CreateParameter("Type", template.Type));
        cmd.Parameters.Add(CreateParameter("FaceValue", template.FaceValue));
        cmd.Parameters.Add(CreateParameter("MinConsumption", template.MinConsumption));
        cmd.Parameters.Add(CreateParameter("TotalIssue", template.TotalIssue));
        cmd.Parameters.Add(CreateParameter("IssuedCount", template.IssuedCount));
        cmd.Parameters.Add(CreateParameter("ValidStartTime", template.ValidStartTime));
        cmd.Parameters.Add(CreateParameter("ValidEndTime", template.ValidEndTime));
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
    
    //修改信息，对应管理后台中“编辑保存”的操作
    public async Task<bool> UpdateTemplateAsync(CouponTemplate template, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE coupon_template 
            SET name = :Name, type = :Type, amount = :FaceValue, min_amount = :MinConsumption, 
                total_count = :TotalIssue, start_time = :ValidStartTime, end_time = :ValidEndTime, status = :Status
            WHERE id = :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        cmd.Parameters.Add(CreateParameter("Name", template.Name));
        cmd.Parameters.Add(CreateParameter("Type", template.Type));
        cmd.Parameters.Add(CreateParameter("FaceValue", template.FaceValue));
        cmd.Parameters.Add(CreateParameter("MinConsumption", template.MinConsumption));
        cmd.Parameters.Add(CreateParameter("TotalIssue", template.TotalIssue));
        cmd.Parameters.Add(CreateParameter("ValidStartTime", template.ValidStartTime));
        cmd.Parameters.Add(CreateParameter("ValidEndTime", template.ValidEndTime));
        cmd.Parameters.Add(CreateParameter("Status", template.Status));
        cmd.Parameters.Add(CreateParameter("Id", template.Id));

        return await cmd.ExecuteNonQueryAsync(cancellationToken) > 0;
    }
    
    //  修改启停状态（上架/下架）
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
            FaceValue = reader.GetDecimal(reader.GetOrdinal("face_value")),
            MinConsumption = reader.GetDecimal(reader.GetOrdinal("min_consumption")),
            TotalIssue = reader.GetInt32(reader.GetOrdinal("total_issue")),
            IssuedCount = reader.GetInt32(reader.GetOrdinal("issued_count")),
            ValidStartTime = reader.GetDateTime(reader.GetOrdinal("valid_start_time")),
            ValidEndTime = reader.GetDateTime(reader.GetOrdinal("valid_end_time")),
            Status = reader.GetInt32(reader.GetOrdinal("status"))
        };
    }

    private static void AddTemplateQueryParameters(DbCommand command, string? keyword, int? status)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            command.Parameters.Add(CreateParameter("Keyword", $"%{keyword}%"));
        }
        if (status.HasValue)
        {
            command.Parameters.Add(CreateParameter("Status", status.Value));
        }
    }

    private static DbParameter CreateParameter(string name, object? value)
    {
        return new OracleParameter(name, value ?? DBNull.Value);
    }
}
