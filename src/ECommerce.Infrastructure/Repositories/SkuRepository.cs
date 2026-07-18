using System.Data.Common;
using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using ECommerce.Shared.Abstractions;

namespace ECommerce.Infrastructure.Repositories;

public interface ISkuRepository
{
    Task<Sku?> GetByIdAsync(long skuId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkuDto>> GetByProductAsync(long productId, CancellationToken cancellationToken = default);

    Task<long> CreateAsync(Sku sku, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(Sku sku, CancellationToken cancellationToken = default);

    Task<int> SetStatusAsync(long skuId, int status, CancellationToken cancellationToken = default);

    Task<int> DeleteIfUnreferencedAsync(long skuId, CancellationToken cancellationToken = default);

    Task<int> LockStockAsync(long skuId, int quantity, string orderNo, CancellationToken cancellationToken = default);

    Task<int> ReleaseStockAsync(long skuId, int quantity, CancellationToken cancellationToken = default);

    Task<int> DeductStockAsync(long skuId, int quantity, CancellationToken cancellationToken = default);
}

public sealed class SkuRepository : ISkuRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public SkuRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Sku?> GetByIdAsync(long skuId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT s."ID", s."PRODUCT_ID", s."SPEC_DESC", s."PRICE", s."ORIGINAL_PRICE", s."STOCK", s."LOCKED_STOCK", s."WARNING_STOCK", s."SKU_IMAGE", s."STATUS", s."CREATED_AT", s."UPDATED_AT", p."STATUS" AS "PRODUCT_STATUS"
            FROM "SKU" s
            INNER JOIN "PRODUCT" p ON p."ID" = s."PRODUCT_ID"
            WHERE s."ID" = :skuId
            """;

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = ":skuId";
        parameter.Value = skuId;
        command.Parameters.Add(parameter);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapFromReader(reader);
    }

    public async Task<IReadOnlyList<SkuDto>> GetByProductAsync(long productId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT \"ID\", \"PRODUCT_ID\", \"SPEC_DESC\", \"PRICE\", \"ORIGINAL_PRICE\", \"STOCK\", \"LOCKED_STOCK\", \"WARNING_STOCK\", \"SKU_IMAGE\", \"STATUS\" FROM \"SKU\" WHERE \"PRODUCT_ID\" = :productId ORDER BY \"CREATED_AT\"";

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = ":productId";
        parameter.Value = productId;
        command.Parameters.Add(parameter);

        var skus = new List<SkuDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            skus.Add(MapToDto(reader));
        }

        return skus;
    }

    public async Task<long> CreateAsync(Sku sku, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO "SKU" ("PRODUCT_ID", "SPEC_DESC", "PRICE", "ORIGINAL_PRICE", "STOCK", "LOCKED_STOCK", "WARNING_STOCK", "SKU_IMAGE", "STATUS", "CREATED_AT", "UPDATED_AT")
            VALUES (:productId, :specDesc, :price, :originalPrice, :stock, :lockedStock, :warningStock, :skuImage, :status, :createdAt, :updatedAt)
            RETURNING "ID" INTO :newId
            """;

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;
        AddParameters(command, sku);

        var newIdParam = command.CreateParameter();
        newIdParam.ParameterName = ":newId";
        newIdParam.DbType = System.Data.DbType.Int64;
        newIdParam.Direction = System.Data.ParameterDirection.Output;
        command.Parameters.Add(newIdParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return OracleValueConverter.ToInt64(newIdParam.Value);
    }

    public async Task<int> UpdateAsync(Sku sku, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE "SKU"
            SET "SPEC_DESC" = :specDesc, "PRICE" = :price, "ORIGINAL_PRICE" = :originalPrice,
                "STOCK" = :stock, "LOCKED_STOCK" = :lockedStock, "WARNING_STOCK" = :warningStock,
                "SKU_IMAGE" = :skuImage, "STATUS" = :status, "UPDATED_AT" = :updatedAt
            WHERE "ID" = :skuId
            """;

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;

        var specDescParam = command.CreateParameter();
        specDescParam.ParameterName = ":specDesc";
        specDescParam.Value = sku.SpecDesc;
        command.Parameters.Add(specDescParam);

        var priceParam = command.CreateParameter();
        priceParam.ParameterName = ":price";
        priceParam.Value = sku.Price;
        command.Parameters.Add(priceParam);

        var originalPriceParam = command.CreateParameter();
        originalPriceParam.ParameterName = ":originalPrice";
        originalPriceParam.Value = sku.OriginalPrice.HasValue ? (object)sku.OriginalPrice.Value : DBNull.Value;
        command.Parameters.Add(originalPriceParam);

        var stockParam = command.CreateParameter();
        stockParam.ParameterName = ":stock";
        stockParam.Value = sku.Stock;
        command.Parameters.Add(stockParam);

        var lockedStockParam = command.CreateParameter();
        lockedStockParam.ParameterName = ":lockedStock";
        lockedStockParam.Value = sku.LockedStock;
        command.Parameters.Add(lockedStockParam);

        var warningStockParam = command.CreateParameter();
        warningStockParam.ParameterName = ":warningStock";
        warningStockParam.Value = sku.WarningStock;
        command.Parameters.Add(warningStockParam);

        var skuImageParam = command.CreateParameter();
        skuImageParam.ParameterName = ":skuImage";
        skuImageParam.Value = string.IsNullOrEmpty(sku.SkuImage) ? DBNull.Value : (object)sku.SkuImage;
        command.Parameters.Add(skuImageParam);

        var statusParam = command.CreateParameter();
        statusParam.ParameterName = ":status";
        statusParam.Value = sku.Status;
        command.Parameters.Add(statusParam);

        var updatedAtParam = command.CreateParameter();
        updatedAtParam.ParameterName = ":updatedAt";
        updatedAtParam.Value = DateTime.Now;
        command.Parameters.Add(updatedAtParam);

        var idParam = command.CreateParameter();
        idParam.ParameterName = ":skuId";
        idParam.Value = sku.Id;
        command.Parameters.Add(idParam);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> SetStatusAsync(long skuId, int status, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "UPDATE \"SKU\" SET \"STATUS\" = :status, \"UPDATED_AT\" = :updatedAt WHERE \"ID\" = :skuId";

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;

        var statusParam = command.CreateParameter();
        statusParam.ParameterName = ":status";
        statusParam.Value = status;
        command.Parameters.Add(statusParam);

        var updatedAtParam = command.CreateParameter();
        updatedAtParam.ParameterName = ":updatedAt";
        updatedAtParam.Value = DateTime.Now;
        command.Parameters.Add(updatedAtParam);

        var idParam = command.CreateParameter();
        idParam.ParameterName = ":skuId";
        idParam.Value = skuId;
        command.Parameters.Add(idParam);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> DeleteIfUnreferencedAsync(long skuId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string deleteSql = """
            DELETE FROM "SKU"
            WHERE "ID" = :skuId
              AND NOT EXISTS (SELECT 1 FROM "INVENTORY_LOG" WHERE "SKU_ID" = "SKU"."ID")
              AND NOT EXISTS (SELECT 1 FROM "CART" WHERE "SKU_ID" = "SKU"."ID")
              AND NOT EXISTS (SELECT 1 FROM "ORDER_ITEM" WHERE "SKU_ID" = "SKU"."ID")
            """;

        using var deleteCmd = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            deleteCmd.Transaction = _unitOfWork.CurrentTransaction;
        }
        deleteCmd.CommandText = deleteSql;

        var skuIdParam = deleteCmd.CreateParameter();
        skuIdParam.ParameterName = ":skuId";
        skuIdParam.Value = skuId;
        deleteCmd.Parameters.Add(skuIdParam);

        return await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> LockStockAsync(long skuId, int quantity, string orderNo, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "UPDATE \"SKU\" SET \"LOCKED_STOCK\" = \"LOCKED_STOCK\" + :quantity, \"UPDATED_AT\" = :updatedAt WHERE \"ID\" = :skuId AND \"STOCK\" - \"LOCKED_STOCK\" >= :quantity";

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;

        var quantityParam = command.CreateParameter();
        quantityParam.ParameterName = ":quantity";
        quantityParam.Value = quantity;
        command.Parameters.Add(quantityParam);

        var updatedAtParam = command.CreateParameter();
        updatedAtParam.ParameterName = ":updatedAt";
        updatedAtParam.Value = DateTime.Now;
        command.Parameters.Add(updatedAtParam);

        var idParam = command.CreateParameter();
        idParam.ParameterName = ":skuId";
        idParam.Value = skuId;
        command.Parameters.Add(idParam);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> ReleaseStockAsync(long skuId, int quantity, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "UPDATE \"SKU\" SET \"LOCKED_STOCK\" = \"LOCKED_STOCK\" - :quantity, \"UPDATED_AT\" = :updatedAt WHERE \"ID\" = :skuId AND \"LOCKED_STOCK\" >= :quantity";

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;

        var quantityParam = command.CreateParameter();
        quantityParam.ParameterName = ":quantity";
        quantityParam.Value = quantity;
        command.Parameters.Add(quantityParam);

        var updatedAtParam = command.CreateParameter();
        updatedAtParam.ParameterName = ":updatedAt";
        updatedAtParam.Value = DateTime.Now;
        command.Parameters.Add(updatedAtParam);

        var idParam = command.CreateParameter();
        idParam.ParameterName = ":skuId";
        idParam.Value = skuId;
        command.Parameters.Add(idParam);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> DeductStockAsync(long skuId, int quantity, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "UPDATE \"SKU\" SET \"STOCK\" = \"STOCK\" - :quantity, \"LOCKED_STOCK\" = \"LOCKED_STOCK\" - :quantity, \"UPDATED_AT\" = :updatedAt WHERE \"ID\" = :skuId AND \"LOCKED_STOCK\" >= :quantity";

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;

        var quantityParam = command.CreateParameter();
        quantityParam.ParameterName = ":quantity";
        quantityParam.Value = quantity;
        command.Parameters.Add(quantityParam);

        var updatedAtParam = command.CreateParameter();
        updatedAtParam.ParameterName = ":updatedAt";
        updatedAtParam.Value = DateTime.Now;
        command.Parameters.Add(updatedAtParam);

        var idParam = command.CreateParameter();
        idParam.ParameterName = ":skuId";
        idParam.Value = skuId;
        command.Parameters.Add(idParam);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(DbCommand command, Sku sku)
    {
        var productIdParam = command.CreateParameter();
        productIdParam.ParameterName = ":productId";
        productIdParam.Value = sku.ProductId;
        command.Parameters.Add(productIdParam);

        var specDescParam = command.CreateParameter();
        specDescParam.ParameterName = ":specDesc";
        specDescParam.Value = sku.SpecDesc;
        command.Parameters.Add(specDescParam);

        var priceParam = command.CreateParameter();
        priceParam.ParameterName = ":price";
        priceParam.Value = sku.Price;
        command.Parameters.Add(priceParam);

        var originalPriceParam = command.CreateParameter();
        originalPriceParam.ParameterName = ":originalPrice";
        originalPriceParam.Value = sku.OriginalPrice.HasValue ? (object)sku.OriginalPrice.Value : DBNull.Value;
        command.Parameters.Add(originalPriceParam);

        var stockParam = command.CreateParameter();
        stockParam.ParameterName = ":stock";
        stockParam.Value = sku.Stock;
        command.Parameters.Add(stockParam);

        var lockedStockParam = command.CreateParameter();
        lockedStockParam.ParameterName = ":lockedStock";
        lockedStockParam.Value = sku.LockedStock;
        command.Parameters.Add(lockedStockParam);

        var warningStockParam = command.CreateParameter();
        warningStockParam.ParameterName = ":warningStock";
        warningStockParam.Value = sku.WarningStock;
        command.Parameters.Add(warningStockParam);

        var skuImageParam = command.CreateParameter();
        skuImageParam.ParameterName = ":skuImage";
        skuImageParam.Value = string.IsNullOrEmpty(sku.SkuImage) ? DBNull.Value : (object)sku.SkuImage;
        command.Parameters.Add(skuImageParam);

        var statusParam = command.CreateParameter();
        statusParam.ParameterName = ":status";
        statusParam.Value = sku.Status;
        command.Parameters.Add(statusParam);

        var createdAtParam = command.CreateParameter();
        createdAtParam.ParameterName = ":createdAt";
        createdAtParam.Value = sku.CreatedAt;
        command.Parameters.Add(createdAtParam);

        var updatedAtParam = command.CreateParameter();
        updatedAtParam.ParameterName = ":updatedAt";
        updatedAtParam.Value = sku.UpdatedAt;
        command.Parameters.Add(updatedAtParam);
    }

    private static Sku MapFromReader(DbDataReader reader)
    {
        return new Sku
        {
            Id = reader.GetInt64(reader.GetOrdinal("ID")),
            ProductId = reader.GetInt64(reader.GetOrdinal("PRODUCT_ID")),
            SpecDesc = reader.GetString(reader.GetOrdinal("SPEC_DESC")),
            Price = reader.GetDecimal(reader.GetOrdinal("PRICE")),
            OriginalPrice = reader.IsDBNull(reader.GetOrdinal("ORIGINAL_PRICE")) ? null : reader.GetDecimal(reader.GetOrdinal("ORIGINAL_PRICE")),
            Stock = reader.GetInt32(reader.GetOrdinal("STOCK")),
            LockedStock = reader.GetInt32(reader.GetOrdinal("LOCKED_STOCK")),
            WarningStock = reader.GetInt32(reader.GetOrdinal("WARNING_STOCK")),
            SkuImage = reader.IsDBNull(reader.GetOrdinal("SKU_IMAGE")) ? null : reader.GetString(reader.GetOrdinal("SKU_IMAGE")),
            Status = reader.GetInt32(reader.GetOrdinal("STATUS")),
            ProductStatus = reader.GetInt32(reader.GetOrdinal("PRODUCT_STATUS")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT"))
        };
    }

    private static SkuDto MapToDto(DbDataReader reader)
    {
        return new SkuDto(
            SkuId: reader.GetInt64(reader.GetOrdinal("ID")),
            ProductId: reader.GetInt64(reader.GetOrdinal("PRODUCT_ID")),
            SpecDescJson: reader.GetString(reader.GetOrdinal("SPEC_DESC")),
            Price: reader.GetDecimal(reader.GetOrdinal("PRICE")),
            OriginalPrice: reader.IsDBNull(reader.GetOrdinal("ORIGINAL_PRICE")) ? null : reader.GetDecimal(reader.GetOrdinal("ORIGINAL_PRICE")),
            Stock: reader.GetInt32(reader.GetOrdinal("STOCK")),
            LockedStock: reader.GetInt32(reader.GetOrdinal("LOCKED_STOCK")),
            WarningStock: reader.GetInt32(reader.GetOrdinal("WARNING_STOCK")),
            SkuImage: reader.IsDBNull(reader.GetOrdinal("SKU_IMAGE")) ? null : reader.GetString(reader.GetOrdinal("SKU_IMAGE")),
            Status: reader.GetInt32(reader.GetOrdinal("STATUS"))
        );
    }
}
