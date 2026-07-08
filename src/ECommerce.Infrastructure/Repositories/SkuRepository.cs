using System.Data.Common;
using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Shared.Abstractions;

namespace ECommerce.Infrastructure.Repositories;

public interface ISkuRepository
{
    Task<Sku?> GetByIdAsync(long skuId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkuDto>> GetByProductAsync(long productId, CancellationToken cancellationToken = default);

    Task<long> CreateAsync(Sku sku, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(Sku sku, CancellationToken cancellationToken = default);

    Task<int> SetStatusAsync(long skuId, int status, CancellationToken cancellationToken = default);

    Task<int> DeleteByProductAsync(long productId, CancellationToken cancellationToken = default);

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
        const string sql = "SELECT id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status, created_at, updated_at FROM SKU WHERE id = :skuId";

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
        const string sql = "SELECT id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status FROM SKU WHERE product_id = :productId ORDER BY created_at";

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
            INSERT INTO SKU (product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status, created_at, updated_at)
            VALUES (:productId, :specDesc, :price, :originalPrice, :stock, :lockedStock, :warningStock, :skuImage, :status, :createdAt, :updatedAt)
            RETURNING id INTO :newId
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
        return Convert.ToInt64(newIdParam.Value);
    }

    public async Task<int> UpdateAsync(Sku sku, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE SKU 
            SET product_id = :productId, spec_desc = :specDesc, price = :price, original_price = :originalPrice, 
                stock = :stock, locked_stock = :lockedStock, warning_stock = :warningStock, sku_image = :skuImage, 
                status = :status, updated_at = :updatedAt
            WHERE id = :skuId
            """;

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;
        AddParameters(command, sku);

        var idParam = command.CreateParameter();
        idParam.ParameterName = ":skuId";
        idParam.Value = sku.Id;
        command.Parameters.Add(idParam);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> SetStatusAsync(long skuId, int status, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "UPDATE SKU SET status = :status, updated_at = :updatedAt WHERE id = :skuId";

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

    public async Task<int> DeleteByProductAsync(long productId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM SKU WHERE product_id = :productId";

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

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> LockStockAsync(long skuId, int quantity, string orderNo, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "UPDATE SKU SET locked_stock = locked_stock + :quantity, updated_at = :updatedAt WHERE id = :skuId AND stock - locked_stock >= :quantity";

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
        const string sql = "UPDATE SKU SET locked_stock = locked_stock - :quantity, updated_at = :updatedAt WHERE id = :skuId AND locked_stock >= :quantity";

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
        const string sql = "UPDATE SKU SET stock = stock - :quantity, locked_stock = locked_stock - :quantity, updated_at = :updatedAt WHERE id = :skuId AND locked_stock >= :quantity";

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
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            ProductId = reader.GetInt64(reader.GetOrdinal("product_id")),
            SpecDesc = reader.GetString(reader.GetOrdinal("spec_desc")),
            Price = reader.GetDecimal(reader.GetOrdinal("price")),
            OriginalPrice = reader.IsDBNull(reader.GetOrdinal("original_price")) ? null : reader.GetDecimal(reader.GetOrdinal("original_price")),
            Stock = reader.GetInt32(reader.GetOrdinal("stock")),
            LockedStock = reader.GetInt32(reader.GetOrdinal("locked_stock")),
            WarningStock = reader.GetInt32(reader.GetOrdinal("warning_stock")),
            SkuImage = reader.IsDBNull(reader.GetOrdinal("sku_image")) ? null : reader.GetString(reader.GetOrdinal("sku_image")),
            Status = reader.GetInt32(reader.GetOrdinal("status")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }

    private static SkuDto MapToDto(DbDataReader reader)
    {
        return new SkuDto(
            SkuId: reader.GetInt64(reader.GetOrdinal("id")),
            ProductId: reader.GetInt64(reader.GetOrdinal("product_id")),
            SpecDescJson: reader.GetString(reader.GetOrdinal("spec_desc")),
            Price: reader.GetDecimal(reader.GetOrdinal("price")),
            OriginalPrice: reader.IsDBNull(reader.GetOrdinal("original_price")) ? null : reader.GetDecimal(reader.GetOrdinal("original_price")),
            Stock: reader.GetInt32(reader.GetOrdinal("stock")),
            LockedStock: reader.GetInt32(reader.GetOrdinal("locked_stock")),
            WarningStock: reader.GetInt32(reader.GetOrdinal("warning_stock")),
            SkuImage: reader.IsDBNull(reader.GetOrdinal("sku_image")) ? null : reader.GetString(reader.GetOrdinal("sku_image")),
            Status: reader.GetInt32(reader.GetOrdinal("status"))
        );
    }
}