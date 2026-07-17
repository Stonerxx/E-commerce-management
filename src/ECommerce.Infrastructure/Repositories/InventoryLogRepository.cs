using System.Data.Common;
using System.Text;
using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
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
        sql.Append("SELECT l.\"ID\", l.\"SKU_ID\", l.\"CHANGE_TYPE\", l.\"CHANGE_QTY\", l.\"BEFORE_STOCK\", l.\"AFTER_STOCK\", l.\"OPERATOR_ID\", l.\"REF_ORDER_ID\", l.\"REMARK\", l.\"CREATED_AT\", s.\"PRODUCT_ID\" AS \"ProductId\", p.\"NAME\" AS \"ProductName\", u.\"USERNAME\" AS \"OperatorName\" ");
        sql.Append("FROM \"INVENTORY_LOG\" l INNER JOIN \"SKU\" s ON s.\"ID\" = l.\"SKU_ID\" INNER JOIN \"PRODUCT\" p ON p.\"ID\" = s.\"PRODUCT_ID\" LEFT JOIN \"USER\" u ON u.\"ID\" = l.\"OPERATOR_ID\" ");

        var conditions = new List<string>();
        if (query.SkuId.HasValue)
        {
            conditions.Add("l.\"SKU_ID\" = :skuId");
        }
        if (!string.IsNullOrWhiteSpace(query.ChangeType))
        {
            conditions.Add("l.\"CHANGE_TYPE\" = :changeType");
        }
        if (query.StartTime.HasValue)
        {
            conditions.Add("l.\"CREATED_AT\" >= :startTime");
        }
        if (query.EndTime.HasValue)
        {
            conditions.Add("l.\"CREATED_AT\" <= :endTime");
        }

        if (conditions.Count > 0)
        {
            sql.Append("WHERE " + string.Join(" AND ", conditions));
        }

        sql.Append(" ORDER BY l.\"CREATED_AT\" DESC");
        
        var countSql = new StringBuilder();
        countSql.Append("SELECT COUNT(*) FROM \"INVENTORY_LOG\" l");
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

        var offset = (query.SafePageIndex - 1) * query.SafePageSize;
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
        pageSizeParam.Value = query.SafePageSize;
        command.Parameters.Add(pageSizeParam);

        var items = new List<InventoryLogDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapToDto(reader));
        }

        return new PagedResult<InventoryLogDto>(items, query.SafePageIndex, query.SafePageSize, totalCount);
    }

    public async Task<long> CreateAsync(InventoryLog log, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO "INVENTORY_LOG" ("SKU_ID", "CHANGE_TYPE", "CHANGE_QTY", "BEFORE_STOCK", "AFTER_STOCK", "OPERATOR_ID", "REF_ORDER_ID", "REMARK", "CREATED_AT")
            VALUES (:skuId, :changeType, :changeQty, :beforeStock, :afterStock, :operatorId, :refOrderId, :remark, :createdAt)
            RETURNING "ID" INTO :newId
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
        return OracleValueConverter.ToInt64(newIdParam.Value);
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
            LogId: reader.GetInt64(reader.GetOrdinal("ID")),
            SkuId: reader.GetInt64(reader.GetOrdinal("SKU_ID")),
            ChangeType: reader.GetString(reader.GetOrdinal("CHANGE_TYPE")),
            ChangeQty: reader.GetInt32(reader.GetOrdinal("CHANGE_QTY")),
            BeforeStock: reader.GetInt32(reader.GetOrdinal("BEFORE_STOCK")),
            AfterStock: reader.GetInt32(reader.GetOrdinal("AFTER_STOCK")),
            OperatorId: reader.IsDBNull(reader.GetOrdinal("OPERATOR_ID")) ? null : reader.GetInt64(reader.GetOrdinal("OPERATOR_ID")),
            RefOrderId: reader.IsDBNull(reader.GetOrdinal("REF_ORDER_ID")) ? null : reader.GetInt64(reader.GetOrdinal("REF_ORDER_ID")),
            Remark: reader.IsDBNull(reader.GetOrdinal("REMARK")) ? null : reader.GetString(reader.GetOrdinal("REMARK")),
            CreatedAt: reader.GetDateTime(reader.GetOrdinal("CREATED_AT")),
            ProductId: reader.GetInt64(reader.GetOrdinal("ProductId")),
            ProductName: reader.IsDBNull(reader.GetOrdinal("ProductName")) ? null : reader.GetString(reader.GetOrdinal("ProductName")),
            OperatorName: reader.IsDBNull(reader.GetOrdinal("OperatorName")) ? null : reader.GetString(reader.GetOrdinal("OperatorName"))
        );
    }
}
