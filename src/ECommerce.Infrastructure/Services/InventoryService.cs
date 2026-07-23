using System.Data.Common;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISkuRepository _skuRepository;
    private readonly IInventoryLogRepository _inventoryLogRepository;

    public InventoryService(
        IUnitOfWork unitOfWork,
        ISkuRepository skuRepository,
        IInventoryLogRepository inventoryLogRepository)
    {
        _unitOfWork = unitOfWork;
        _skuRepository = skuRepository;
        _inventoryLogRepository = inventoryLogRepository;
    }

    public async Task AdjustAsync(long skuId, InventoryAdjustRequest request, long operatorId, CancellationToken cancellationToken = default)
    {
        if (request.ChangeQty == 0)
        {
            throw new BusinessException("INVENTORY_CHANGE_ZERO", "库存调整数量不能为0");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var sku = await _skuRepository.GetByIdAsync(skuId, cancellationToken);
            if (sku == null)
            {
                throw new BusinessException("SKU_NOT_FOUND", "SKU不存在");
            }

            var beforeStock = sku.Stock;
            var afterStock = sku.Stock + request.ChangeQty;

            if (afterStock < 0 || afterStock < sku.LockedStock)
            {
                throw new BusinessException("INVENTORY_INSUFFICIENT", "库存不足，调整后库存不能小于锁定库存");
            }

            sku.Stock = afterStock;
            sku.UpdatedAt = DateTime.Now;
            await _skuRepository.UpdateAsync(sku, cancellationToken);

            var log = new InventoryLog
            {
                SkuId = skuId,
                ChangeType = InventoryChangeType.Adjust,
                ChangeQty = request.ChangeQty,
                BeforeStock = beforeStock,
                AfterStock = afterStock,
                OperatorId = operatorId,
                RefOrderId = null,
                Remark = request.Remark,
                CreatedAt = DateTime.Now
            };
            await _inventoryLogRepository.CreateAsync(log, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task LockForOrderAsync(long orderId, IReadOnlyList<OrderSkuQuantity> items, CancellationToken cancellationToken = default)
    {
        if (items == null || items.Count == 0)
        {
            return;
        }

        var ownsTransaction = _unitOfWork.CurrentTransaction is null;
        if (ownsTransaction)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            foreach (var item in items)
            {
                var affectedRows = await _skuRepository.LockStockAsync(
                    item.SkuId,
                    item.Quantity,
                    orderId.ToString(),
                    cancellationToken);
                if (affectedRows != 1)
                {
                    throw new BusinessException("INVENTORY_INSUFFICIENT", $"SKU {item.SkuId} 库存不足或状态已变化");
                }

                var sku = await GetUpdatedSkuAsync(item.SkuId, cancellationToken);

                var log = new InventoryLog
                {
                    SkuId = item.SkuId,
                    ChangeType = InventoryChangeType.OrderLock,
                    ChangeQty = item.Quantity,
                    BeforeStock = sku.Stock,
                    AfterStock = sku.Stock,
                    OperatorId = null,
                    RefOrderId = orderId,
                    Remark = $"订单锁定库存: {orderId}",
                    CreatedAt = DateTime.Now
                };
                await _inventoryLogRepository.CreateAsync(log, cancellationToken);
            }

            if (ownsTransaction)
            {
                await _unitOfWork.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (ownsTransaction)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    public async Task ReleaseForCancelledOrderAsync(long orderId, IReadOnlyList<OrderSkuQuantity> items, CancellationToken cancellationToken = default)
    {
        if (items == null || items.Count == 0)
        {
            return;
        }

        var ownsTransaction = _unitOfWork.CurrentTransaction is null;
        if (ownsTransaction)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            foreach (var item in items)
            {
                var affectedRows = await _skuRepository.ReleaseStockAsync(item.SkuId, item.Quantity, cancellationToken);
                if (affectedRows != 1)
                {
                    throw new BusinessException("INVENTORY_LOCKED_INSUFFICIENT", $"SKU {item.SkuId} 锁定库存不足或状态已变化");
                }

                var sku = await GetUpdatedSkuAsync(item.SkuId, cancellationToken);

                var log = new InventoryLog
                {
                    SkuId = item.SkuId,
                    ChangeType = InventoryChangeType.OrderRelease,
                    ChangeQty = -item.Quantity,
                    BeforeStock = sku.Stock,
                    AfterStock = sku.Stock,
                    OperatorId = null,
                    RefOrderId = orderId,
                    Remark = $"订单取消释放库存: {orderId}",
                    CreatedAt = DateTime.Now
                };
                await _inventoryLogRepository.CreateAsync(log, cancellationToken);
            }

            if (ownsTransaction)
            {
                await _unitOfWork.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (ownsTransaction)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    public async Task DeductForPaidOrderAsync(long orderId, IReadOnlyList<OrderSkuQuantity> items, CancellationToken cancellationToken = default)
    {
        if (items == null || items.Count == 0)
        {
            return;
        }

        var ownsTransaction = _unitOfWork.CurrentTransaction is null;
        if (ownsTransaction)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            foreach (var item in items)
            {
                var affectedRows = await _skuRepository.DeductStockAsync(item.SkuId, item.Quantity, cancellationToken);
                if (affectedRows != 1)
                {
                    throw new BusinessException("INVENTORY_LOCKED_INSUFFICIENT", $"SKU {item.SkuId} 锁定库存不足或状态已变化");
                }

                var sku = await GetUpdatedSkuAsync(item.SkuId, cancellationToken);

                var log = new InventoryLog
                {
                    SkuId = item.SkuId,
                    ChangeType = InventoryChangeType.OrderDeduct,
                    ChangeQty = -item.Quantity,
                    BeforeStock = sku.Stock + item.Quantity,
                    AfterStock = sku.Stock,
                    OperatorId = null,
                    RefOrderId = orderId,
                    Remark = $"订单支付扣减库存: {orderId}",
                    CreatedAt = DateTime.Now
                };
                await _inventoryLogRepository.CreateAsync(log, cancellationToken);
            }

            if (ownsTransaction)
            {
                await _unitOfWork.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (ownsTransaction)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    public async Task<PagedResult<InventoryLogDto>> SearchLogsAsync(InventoryLogQuery query, CancellationToken cancellationToken = default)
    {
        return await _inventoryLogRepository.SearchLogsAsync(query, cancellationToken);
    }

    public async Task<PagedResult<InventoryWarningDto>> SearchWarningsAsync(InventoryWarningQuery query, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);

        var sql = new System.Text.StringBuilder();
        sql.Append("SELECT sku_id AS id, product_id, product_name, spec_desc, stock, locked_stock, warning_stock ");
        sql.Append("FROM V_PRODUCT_INVENTORY ");
        sql.Append("WHERE is_warning = 1 AND sku_status = 1 AND product_status = 1 ");
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            sql.Append("AND (LOWER(product_name) LIKE :keyword OR TO_CHAR(sku_id) LIKE :keyword OR LOWER(spec_desc) LIKE :keyword) ");
        }
        sql.Append("ORDER BY available_stock ASC ");

        var countSql = """
            SELECT COUNT(*)
            FROM V_PRODUCT_INVENTORY
            WHERE is_warning = 1
              AND sku_status = 1
              AND product_status = 1
            """;
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            countSql += " AND (LOWER(product_name) LIKE :keyword OR TO_CHAR(sku_id) LIKE :keyword OR LOWER(spec_desc) LIKE :keyword)";
        }

        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = countSql;
        AddWarningKeywordParameter(countCommand, query.Keyword);
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        var offset = (query.SafePageIndex - 1) * query.SafePageSize;
        sql.Append("OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY");

        using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();
        AddWarningKeywordParameter(command, query.Keyword);

        var offsetParam = command.CreateParameter();
        offsetParam.ParameterName = ":offset";
        offsetParam.Value = offset;
        command.Parameters.Add(offsetParam);

        var pageSizeParam = command.CreateParameter();
        pageSizeParam.ParameterName = ":pageSize";
        pageSizeParam.Value = query.SafePageSize;
        command.Parameters.Add(pageSizeParam);

        var items = new List<InventoryWarningDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapToWarningDto(reader));
        }

        return new PagedResult<InventoryWarningDto>(items, query.SafePageIndex, query.SafePageSize, totalCount);
    }

    private static void AddWarningKeywordParameter(DbCommand command, string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return;
        }

        var parameter = command.CreateParameter();
        parameter.ParameterName = ":keyword";
        parameter.Value = $"%{keyword.Trim().ToLowerInvariant()}%";
        command.Parameters.Add(parameter);
    }

    private static InventoryWarningDto MapToWarningDto(DbDataReader reader)
    {
        return new InventoryWarningDto(
            SkuId: reader.GetInt64(reader.GetOrdinal("id")),
            ProductId: reader.GetInt64(reader.GetOrdinal("product_id")),
            ProductName: reader.GetString(reader.GetOrdinal("product_name")),
            SpecDescJson: reader.GetString(reader.GetOrdinal("spec_desc")),
            Stock: reader.GetInt32(reader.GetOrdinal("stock")),
            LockedStock: reader.GetInt32(reader.GetOrdinal("locked_stock")),
            WarningStock: reader.GetInt32(reader.GetOrdinal("warning_stock"))
        );
    }

    private async Task<Sku> GetUpdatedSkuAsync(long skuId, CancellationToken cancellationToken)
    {
        return await _skuRepository.GetByIdAsync(skuId, cancellationToken)
            ?? throw new BusinessException("SKU_NOT_FOUND", $"SKU {skuId} 不存在");
    }
}
