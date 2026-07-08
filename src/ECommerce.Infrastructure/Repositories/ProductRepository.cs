using System.Data.Common;
using System.Text;
using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;

namespace ECommerce.Infrastructure.Repositories;

public interface IProductRepository
{
    Task<PagedResult<ProductListItemDto>> SearchAsync(ProductQuery query, CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(long productId, CancellationToken cancellationToken = default);

    Task<long> CreateAsync(Product product, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(Product product, CancellationToken cancellationToken = default);

    Task<int> SetStatusAsync(long productId, int status, CancellationToken cancellationToken = default);

    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default);

    Task<int> IncrementSalesCountAsync(long productId, int quantity, CancellationToken cancellationToken = default);

    Task<int> IncrementViewCountAsync(long productId, CancellationToken cancellationToken = default);

    Task<bool> HasSkusAsync(long productId, CancellationToken cancellationToken = default);
}

public sealed class ProductRepository : IProductRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<ProductListItemDto>> SearchAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        
        var sql = new StringBuilder();
        sql.Append("SELECT p.id, p.category_id, p.name, p.main_image, p.price_min, p.sales_count, p.avg_rating, p.status ");
        sql.Append("FROM PRODUCT p ");
        
        var conditions = new List<string>();
        if (query.CategoryId.HasValue)
        {
            conditions.Add("p.category_id = :categoryId");
        }
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            conditions.Add("p.name LIKE :keyword");
        }
        if (query.Status.HasValue)
        {
            conditions.Add("p.status = :status");
        }
        
        if (conditions.Count > 0)
        {
            sql.Append("WHERE " + string.Join(" AND ", conditions));
        }
        
        sql.Append(" ORDER BY p.created_at DESC");
        
        var countSql = new StringBuilder();
        countSql.Append("SELECT COUNT(*) FROM PRODUCT p");
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

        var items = new List<ProductListItemDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapToListItemDto(reader));
        }

        return new PagedResult<ProductListItemDto>(items, query.PageIndex, query.PageSize, totalCount);
    }

    public async Task<Product?> GetByIdAsync(long productId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT id, category_id, name, description, main_image, status, price_min, sales_count, view_count, avg_rating, created_at, updated_at FROM PRODUCT WHERE id = :productId";

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

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapFromReader(reader);
    }

    public async Task<long> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO PRODUCT (category_id, name, description, main_image, status, price_min, sales_count, view_count, avg_rating, created_at, updated_at)
            VALUES (:categoryId, :name, :description, :mainImage, :status, :priceMin, :salesCount, :viewCount, :avgRating, :createdAt, :updatedAt)
            RETURNING id INTO :newId
            """;

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;
        AddProductParameters(command, product);

        var newIdParam = command.CreateParameter();
        newIdParam.ParameterName = ":newId";
        newIdParam.DbType = System.Data.DbType.Int64;
        newIdParam.Direction = System.Data.ParameterDirection.Output;
        command.Parameters.Add(newIdParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return Convert.ToInt64(newIdParam.Value);
    }

    public async Task<int> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE PRODUCT 
            SET category_id = :categoryId, name = :name, description = :description, main_image = :mainImage, 
                status = :status, price_min = :priceMin, updated_at = :updatedAt
            WHERE id = :productId
            """;

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;
        AddProductParameters(command, product);

        var idParam = command.CreateParameter();
        idParam.ParameterName = ":productId";
        idParam.Value = product.Id;
        command.Parameters.Add(idParam);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> SetStatusAsync(long productId, int status, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "UPDATE PRODUCT SET status = :status, updated_at = :updatedAt WHERE id = :productId";

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
        idParam.ParameterName = ":productId";
        idParam.Value = productId;
        command.Parameters.Add(idParam);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(*) FROM \"CATEGORY\" WHERE id = :categoryId AND status = 1";

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = ":categoryId";
        parameter.Value = categoryId;
        command.Parameters.Add(parameter);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }

    public async Task<int> IncrementSalesCountAsync(long productId, int quantity, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "UPDATE PRODUCT SET sales_count = sales_count + :quantity, updated_at = :updatedAt WHERE id = :productId";

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
        idParam.ParameterName = ":productId";
        idParam.Value = productId;
        command.Parameters.Add(idParam);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> IncrementViewCountAsync(long productId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "UPDATE PRODUCT SET view_count = view_count + 1, updated_at = :updatedAt WHERE id = :productId";

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;

        var updatedAtParam = command.CreateParameter();
        updatedAtParam.ParameterName = ":updatedAt";
        updatedAtParam.Value = DateTime.Now;
        command.Parameters.Add(updatedAtParam);

        var idParam = command.CreateParameter();
        idParam.ParameterName = ":productId";
        idParam.Value = productId;
        command.Parameters.Add(idParam);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> HasSkusAsync(long productId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(*) FROM SKU WHERE product_id = :productId";

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

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }

    private static void AddSearchParameters(DbCommand command, ProductQuery query)
    {
        if (query.CategoryId.HasValue)
        {
            var param = command.CreateParameter();
            param.ParameterName = ":categoryId";
            param.Value = query.CategoryId.Value;
            command.Parameters.Add(param);
        }
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var param = command.CreateParameter();
            param.ParameterName = ":keyword";
            param.Value = $"%{query.Keyword}%";
            command.Parameters.Add(param);
        }
        if (query.Status.HasValue)
        {
            var param = command.CreateParameter();
            param.ParameterName = ":status";
            param.Value = query.Status.Value;
            command.Parameters.Add(param);
        }
    }

    private static void AddProductParameters(DbCommand command, Product product)
    {
        var categoryIdParam = command.CreateParameter();
        categoryIdParam.ParameterName = ":categoryId";
        categoryIdParam.Value = product.CategoryId;
        command.Parameters.Add(categoryIdParam);

        var nameParam = command.CreateParameter();
        nameParam.ParameterName = ":name";
        nameParam.Value = product.Name;
        command.Parameters.Add(nameParam);

        var descriptionParam = command.CreateParameter();
        descriptionParam.ParameterName = ":description";
        descriptionParam.Value = string.IsNullOrEmpty(product.Description) ? DBNull.Value : (object)product.Description;
        command.Parameters.Add(descriptionParam);

        var mainImageParam = command.CreateParameter();
        mainImageParam.ParameterName = ":mainImage";
        mainImageParam.Value = product.MainImage;
        command.Parameters.Add(mainImageParam);

        var statusParam = command.CreateParameter();
        statusParam.ParameterName = ":status";
        statusParam.Value = product.Status;
        command.Parameters.Add(statusParam);

        var priceMinParam = command.CreateParameter();
        priceMinParam.ParameterName = ":priceMin";
        priceMinParam.Value = product.PriceMin;
        command.Parameters.Add(priceMinParam);

        var salesCountParam = command.CreateParameter();
        salesCountParam.ParameterName = ":salesCount";
        salesCountParam.Value = product.SalesCount;
        command.Parameters.Add(salesCountParam);

        var viewCountParam = command.CreateParameter();
        viewCountParam.ParameterName = ":viewCount";
        viewCountParam.Value = product.ViewCount;
        command.Parameters.Add(viewCountParam);

        var avgRatingParam = command.CreateParameter();
        avgRatingParam.ParameterName = ":avgRating";
        avgRatingParam.Value = product.AvgRating;
        command.Parameters.Add(avgRatingParam);

        var createdAtParam = command.CreateParameter();
        createdAtParam.ParameterName = ":createdAt";
        createdAtParam.Value = product.CreatedAt;
        command.Parameters.Add(createdAtParam);

        var updatedAtParam = command.CreateParameter();
        updatedAtParam.ParameterName = ":updatedAt";
        updatedAtParam.Value = product.UpdatedAt;
        command.Parameters.Add(updatedAtParam);
    }

    private static Product MapFromReader(DbDataReader reader)
    {
        return new Product
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("category_id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
            MainImage = reader.GetString(reader.GetOrdinal("main_image")),
            Status = reader.GetInt32(reader.GetOrdinal("status")),
            PriceMin = reader.GetDecimal(reader.GetOrdinal("price_min")),
            SalesCount = reader.GetInt32(reader.GetOrdinal("sales_count")),
            ViewCount = reader.GetInt32(reader.GetOrdinal("view_count")),
            AvgRating = reader.GetDecimal(reader.GetOrdinal("avg_rating")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }

    private static ProductListItemDto MapToListItemDto(DbDataReader reader)
    {
        return new ProductListItemDto(
            ProductId: reader.GetInt64(reader.GetOrdinal("id")),
            CategoryId: reader.GetInt32(reader.GetOrdinal("category_id")),
            Name: reader.GetString(reader.GetOrdinal("name")),
            MainImage: reader.GetString(reader.GetOrdinal("main_image")),
            PriceMin: reader.GetDecimal(reader.GetOrdinal("price_min")),
            SalesCount: reader.GetInt32(reader.GetOrdinal("sales_count")),
            AvgRating: reader.GetDecimal(reader.GetOrdinal("avg_rating")),
            Status: reader.GetInt32(reader.GetOrdinal("status"))
        );
    }
}