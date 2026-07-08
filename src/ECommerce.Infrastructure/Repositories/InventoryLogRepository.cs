using System.Data.Common;
using System.Text;
using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;

namespace ECommerce.Infrastructure.Repositories;

public interface IInventoryLogRepository
{
    Task<PagedResult<InventoryLogDto>> SearchLogsAsync(InventoryLogQuery query, CancellationToken cancellationToken = default);

    Task<long> CreateAsync(InventoryLog log, CancellationToken cancellationToken = default);
}

public sealed class InventoryLogRepository : IInventoryLogRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public InventoryLogRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<InventoryLogDto>> SearchLogsAsync(InventoryLogQuery query, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);

        var sql = new StringBuilder();
        sql.Append("SELECT id, sku_id, change_type, change_qty, before_stock, after_stock, operator_id, ref_order_id, remark, created_at ");
        sql.Append("FROM INVENTORY_LOG ");

        var conditions = new List<string>();
        if (query.SkuId.HasValue)
        {
            conditions.Add("sku_id = :skuId");
        }
        if (!string.IsNullOrWhiteSpace(query.ChangeType))
        {
            conditions.Add("change_type = :changeType");
        }
        if (query.StartTime.HasValue)
        {
            conditions.Add("created_at >= :startTime");
        }
        if (query.EndTime.HasValue)
        {
            conditions.Add("created_at <= :endTime");
        }

        if (conditions.Count > 0)
        {
            sql.Append("WHERE " + string.Join(" AND ", conditions));
        }

        sql.Append(" ORDER BY created_at DESC");

        var countSql = new StringBuilder();
        countSql.Append("SELECT COUNT(*) FROM INVENTORY_LOG");
        if (conditions.Count > 0)
        {
            countSql.Append(" WHERE " + string.Join(" AND ", conditions));
        }

        using var countCommand = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            countCommand.Transaction = _unitOfWork.CurrentTransaction;
        }
        countCommand.CommandText = countSql.ToString();
        AddSearchParameters(countCommand, query);

        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        var offset = (query.PageIndex - 1) * query.PageSize;
        sql.Append(" OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY");

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql.ToString();
        AddSearchParameters(command, query);

        var offsetParam = command.CreateParameter();
        offsetParam.ParameterName = ":offset";
        offsetParam.Value = offset;
        command.Parameters.Add(offsetParam);

        var pageSizeParam = command.CreateParameter();
        pageSizeParam.ParameterName = ":pageSize";
        pageSizeParam.Value = query.PageSize;
        command.Parameters.Add(pageSizeParam);

        var items = new List<InventoryLogDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapToDto(reader));
        }

        return new PagedResult<InventoryLogDto>(items, query.PageIndex, query.PageSize, totalCount);
    }

    public async Task<long> CreateAsync(InventoryLog log, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO INVENTORY_LOG (sku_id, change_type, change_qty, before_stock, after_stock, operator_id, ref_order_id, remark, created_at)
            VALUES (:skuId, :changeType, :changeQty, :beforeStock, :afterStock, :operatorId, :refOrderId, :remark, :createdAt)
            RETURNING id INTO :newId
            """;

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;
        AddParameters(command, log);

        var newIdParam = command.CreateParameter();
        newIdParam.ParameterName = ":newId";
        newIdParam.DbType = System.Data.DbType.Int64;
        newIdParam.Direction = System.Data.ParameterDirection.Output;
        command.Parameters.Add(newIdParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return Convert.ToInt64(newIdParam.Value);
    }

    private static void AddSearchParameters(DbCommand command, InventoryLogQuery query)
    {
        if (query.SkuId.HasValue)
        {
            var param = command.CreateParameter();
            param.ParameterName = ":skuId";
            param.Value = query.SkuId.Value;
            command.Parameters.Add(param);
        }
        if (!string.IsNullOrWhiteSpace(query.ChangeType))
        {
            var param = command.CreateParameter();
            param.ParameterName = ":changeType";
            param.Value = query.ChangeType;
            command.Parameters.Add(param);
        }
        if (query.StartTime.HasValue)
        {
            var param = command.CreateParameter();
            param.ParameterName = ":startTime";
            param.Value = query.StartTime.Value;
            command.Parameters.Add(param);
        }
        if (query.EndTime.HasValue)
        {
            var param = command.CreateParameter();
            param.ParameterName = ":endTime";
            param.Value = query.EndTime.Value;
            command.Parameters.Add(param);
        }
    }

    private static void AddParameters(DbCommand command, InventoryLog log)
    {
        var skuIdParam = command.CreateParameter();
        skuIdParam.ParameterName = ":skuId";
        skuIdParam.Value = log.SkuId;
        command.Parameters.Add(skuIdParam);

        var changeTypeParam = command.CreateParameter();
        changeTypeParam.ParameterName = ":changeType";
        changeTypeParam.Value = log.ChangeType;
        command.Parameters.Add(changeTypeParam);

        var changeQtyParam = command.CreateParameter();
        changeQtyParam.ParameterName = ":changeQty";
        changeQtyParam.Value = log.ChangeQty;
        command.Parameters.Add(changeQtyParam);

        var beforeStockParam = command.CreateParameter();
        beforeStockParam.ParameterName = ":beforeStock";
        beforeStockParam.Value = log.BeforeStock;
        command.Parameters.Add(beforeStockParam);

        var afterStockParam = command.CreateParameter();
        afterStockParam.ParameterName = ":afterStock";
        afterStockParam.Value = log.AfterStock;
        command.Parameters.Add(afterStockParam);

        var operatorIdParam = command.CreateParameter();
        operatorIdParam.ParameterName = ":operatorId";
        operatorIdParam.Value = log.OperatorId.HasValue ? (object)log.OperatorId.Value : DBNull.Value;
        command.Parameters.Add(operatorIdParam);

        var refOrderIdParam = command.CreateParameter();
        refOrderIdParam.ParameterName = ":refOrderId";
        refOrderIdParam.Value = log.RefOrderId.HasValue ? (object)log.RefOrderId.Value : DBNull.Value;
        command.Parameters.Add(refOrderIdParam);

        var remarkParam = command.CreateParameter();
        remarkParam.ParameterName = ":remark";
        remarkParam.Value = string.IsNullOrEmpty(log.Remark) ? DBNull.Value : (object)log.Remark;
        command.Parameters.Add(remarkParam);

        var createdAtParam = command.CreateParameter();
        createdAtParam.ParameterName = ":createdAt";
        createdAtParam.Value = log.CreatedAt;
        command.Parameters.Add(createdAtParam);
    }

    private static InventoryLogDto MapToDto(DbDataReader reader)
    {
        return new InventoryLogDto(
            LogId: reader.GetInt64(reader.GetOrdinal("id")),
            SkuId: reader.GetInt64(reader.GetOrdinal("sku_id")),
            ChangeType: reader.GetString(reader.GetOrdinal("change_type")),
            ChangeQty: reader.GetInt32(reader.GetOrdinal("change_qty")),
            BeforeStock: reader.GetInt32(reader.GetOrdinal("before_stock")),
            AfterStock: reader.GetInt32(reader.GetOrdinal("after_stock")),
            OperatorId: reader.IsDBNull(reader.GetOrdinal("operator_id")) ? null : reader.GetInt64(reader.GetOrdinal("operator_id")),
            RefOrderId: reader.IsDBNull(reader.GetOrdinal("ref_order_id")) ? null : reader.GetInt64(reader.GetOrdinal("ref_order_id")),
            Remark: reader.IsDBNull(reader.GetOrdinal("remark")) ? null : reader.GetString(reader.GetOrdinal("remark")),
            CreatedAt: reader.GetDateTime(reader.GetOrdinal("created_at"))
        );
    }
}