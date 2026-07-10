using System.Data.Common;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public static class InventoryChangeTypes
{
    public const string AdminAdjust = "ADJUST";
    public const string OrderLock = "ADJUST";
    public const string OrderRelease = "CANCEL";
    public const string OrderDeduct = "SALE";
}

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

            if (afterStock < 0)
            {
                throw new BusinessException("INVENTORY_INSUFFICIENT", "库存不足，调整后库存不能为负数");
            }

            sku.Stock = afterStock;
            sku.UpdatedAt = DateTime.Now;
            await _skuRepository.UpdateAsync(sku, cancellationToken);

            var log = new InventoryLog
            {
                SkuId = skuId,
                ChangeType = InventoryChangeTypes.AdminAdjust,
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
                var sku = await _skuRepository.GetByIdAsync(item.SkuId, cancellationToken);
                if (sku == null)
                {
                    throw new BusinessException("SKU_NOT_FOUND", $"SKU {item.SkuId} 不存在");
                }

                var availableStock = sku.Stock - sku.LockedStock;
                if (availableStock < item.Quantity)
                {
                    throw new BusinessException("INVENTORY_INSUFFICIENT", $"SKU {sku.Id} 库存不足，可用库存: {availableStock}，需要: {item.Quantity}");
                }

                var beforeStock = sku.Stock;
                sku.LockedStock += item.Quantity;
                sku.UpdatedAt = DateTime.Now;
                await _skuRepository.UpdateAsync(sku, cancellationToken);

                var log = new InventoryLog
                {
                    SkuId = item.SkuId,
                    ChangeType = InventoryChangeTypes.OrderLock,
                    ChangeQty = item.Quantity,
                    BeforeStock = beforeStock,
                    AfterStock = beforeStock,
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
                var sku = await _skuRepository.GetByIdAsync(item.SkuId, cancellationToken);
                if (sku == null)
                {
                    throw new BusinessException("SKU_NOT_FOUND", $"SKU {item.SkuId} 不存在");
                }

                if (sku.LockedStock < item.Quantity)
                {
                    throw new BusinessException("INVENTORY_LOCKED_INSUFFICIENT", $"SKU {sku.Id} 锁定库存不足，锁定库存: {sku.LockedStock}，需要释放: {item.Quantity}");
                }

                var beforeStock = sku.Stock;
                sku.LockedStock -= item.Quantity;
                sku.UpdatedAt = DateTime.Now;
                await _skuRepository.UpdateAsync(sku, cancellationToken);

                var log = new InventoryLog
                {
                    SkuId = item.SkuId,
                    ChangeType = InventoryChangeTypes.OrderRelease,
                    ChangeQty = -item.Quantity,
                    BeforeStock = beforeStock,
                    AfterStock = beforeStock,
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
                var sku = await _skuRepository.GetByIdAsync(item.SkuId, cancellationToken);
                if (sku == null)
                {
                    throw new BusinessException("SKU_NOT_FOUND", $"SKU {item.SkuId} 不存在");
                }

                if (sku.LockedStock < item.Quantity)
                {
                    throw new BusinessException("INVENTORY_LOCKED_INSUFFICIENT", $"SKU {sku.Id} 锁定库存不足，锁定库存: {sku.LockedStock}，需要扣减: {item.Quantity}");
                }

                var beforeStock = sku.Stock;
                sku.Stock -= item.Quantity;
                sku.LockedStock -= item.Quantity;
                sku.UpdatedAt = DateTime.Now;
                await _skuRepository.UpdateAsync(sku, cancellationToken);

                var log = new InventoryLog
                {
                    SkuId = item.SkuId,
                    ChangeType = InventoryChangeTypes.OrderDeduct,
                    ChangeQty = -item.Quantity,
                    BeforeStock = beforeStock,
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

    public async Task<PagedResult<InventoryWarningDto>> SearchWarningsAsync(PageQuery query, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);

        var sql = new System.Text.StringBuilder();
        sql.Append("SELECT s.id, s.product_id, p.name as product_name, s.spec_desc, s.stock, s.locked_stock, s.warning_stock ");
        sql.Append("FROM SKU s ");
        sql.Append("INNER JOIN PRODUCT p ON s.product_id = p.id ");
        sql.Append("WHERE s.stock <= s.warning_stock AND s.status = 1 ");
        sql.Append("ORDER BY s.stock ASC ");

        const string countSql = "SELECT COUNT(*) FROM SKU WHERE stock <= warning_stock AND status = 1";

        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = countSql;
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        var offset = (query.PageIndex - 1) * query.PageSize;
        sql.Append("OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY");

        using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();

        var offsetParam = command.CreateParameter();
        offsetParam.ParameterName = ":offset";
        offsetParam.Value = offset;
        command.Parameters.Add(offsetParam);

        var pageSizeParam = command.CreateParameter();
        pageSizeParam.ParameterName = ":pageSize";
        pageSizeParam.Value = query.PageSize;
        command.Parameters.Add(pageSizeParam);

        var items = new List<InventoryWarningDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapToWarningDto(reader));
        }

        return new PagedResult<InventoryWarningDto>(items, query.PageIndex, query.PageSize, totalCount);
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
}
