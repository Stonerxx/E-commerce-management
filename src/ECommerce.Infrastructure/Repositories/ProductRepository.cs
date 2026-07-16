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

    Task<PagedResult<ProductListItemDto>> SearchPublicAsync(ProductQuery query, CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(long productId, CancellationToken cancellationToken = default);

    Task<long> CreateAsync(Product product, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(Product product, CancellationToken cancellationToken = default);

    Task<int> SetStatusAsync(long productId, int status, CancellationToken cancellationToken = default);

    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default);

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
        return await SearchCoreAsync(query, null, cancellationToken);
    }

    public async Task<PagedResult<ProductListItemDto>> SearchPublicAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        return await SearchCoreAsync(query, new[] { 1, 2 }, cancellationToken);
    }

    private async Task<PagedResult<ProductListItemDto>> SearchCoreAsync(
        ProductQuery query,
        IReadOnlyList<int>? fixedStatuses,
        CancellationToken cancellationToken)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        
        var sql = new StringBuilder();
        sql.Append("SELECT p.\"ID\", p.\"CATEGORY_ID\", p.\"NAME\", p.\"MAIN_IMAGE\", p.\"PRICE_MIN\", p.\"SALES_COUNT\", p.\"AVG_RATING\", p.\"STATUS\" ");
        sql.Append("FROM \"PRODUCT\" p ");
        
        var conditions = new List<string>();
        if (query.CategoryId.HasValue)
        {
            conditions.Add("p.\"CATEGORY_ID\" = :categoryId");
        }
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            conditions.Add("p.\"NAME\" LIKE :keyword");
        }
        if (fixedStatuses is not null)
        {
            conditions.Add($"p.\"STATUS\" IN ({string.Join(", ", fixedStatuses)})");
        }
        else if (query.Status.HasValue)
        {
            conditions.Add("p.\"STATUS\" = :status");
        }
        
        if (conditions.Count > 0)
        {
            sql.Append("WHERE " + string.Join(" AND ", conditions));
        }
        
        sql.Append(" ORDER BY p.\"CREATED_AT\" DESC");
        
        var countSql = new StringBuilder();
        countSql.Append("SELECT COUNT(*) FROM \"PRODUCT\" p");
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
        AddSearchParameters(countCommand, query, fixedStatuses is null);
        
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        var offset = (query.SafePageIndex - 1) * query.SafePageSize;
        sql.Append(" OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY");

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql.ToString();
        AddSearchParameters(command, query, fixedStatuses is null);
        
        var offsetParam = command.CreateParameter();
        offsetParam.ParameterName = ":offset";
        offsetParam.Value = offset;
        command.Parameters.Add(offsetParam);
        
        var pageSizeParam = command.CreateParameter();
        pageSizeParam.ParameterName = ":pageSize";
        pageSizeParam.Value = query.SafePageSize;
        command.Parameters.Add(pageSizeParam);

        var items = new List<ProductListItemDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapToListItemDto(reader));
        }

        return new PagedResult<ProductListItemDto>(items, query.SafePageIndex, query.SafePageSize, totalCount);
    }

    public async Task<Product?> GetByIdAsync(long productId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT \"ID\", \"CATEGORY_ID\", \"NAME\", \"DESCRIPTION\", \"MAIN_IMAGE\", \"STATUS\", \"PRICE_MIN\", \"SALES_COUNT\", \"VIEW_COUNT\", \"AVG_RATING\", \"CREATED_AT\", \"UPDATED_AT\" FROM \"PRODUCT\" WHERE \"ID\" = :productId";

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
            INSERT INTO "PRODUCT" ("CATEGORY_ID", "NAME", "DESCRIPTION", "MAIN_IMAGE", "STATUS", "PRICE_MIN", "SALES_COUNT", "VIEW_COUNT", "AVG_RATING", "CREATED_AT", "UPDATED_AT")
            VALUES (:categoryId, :name, :description, :mainImage, :status, :priceMin, :salesCount, :viewCount, :avgRating, :createdAt, :updatedAt)
            RETURNING "ID" INTO :newId
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
            UPDATE "PRODUCT"
            SET "CATEGORY_ID" = :categoryId, "NAME" = :name, "DESCRIPTION" = :description, "MAIN_IMAGE" = :mainImage,
                "STATUS" = :status, "PRICE_MIN" = :priceMin, "UPDATED_AT" = :updatedAt
            WHERE "ID" = :productId
            """;

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;

        AddUpdateParameters(command, product);

        var idParam = command.CreateParameter();
        idParam.ParameterName = ":productId";
        idParam.Value = product.Id;
        command.Parameters.Add(idParam);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> SetStatusAsync(long productId, int status, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "UPDATE \"PRODUCT\" SET \"STATUS\" = :status, \"UPDATED_AT\" = :updatedAt WHERE \"ID\" = :productId";

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
        const string sql = "SELECT COUNT(*) FROM \"CATEGORY\" WHERE \"ID\" = :categoryId AND \"STATUS\" = 1";

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

    public async Task<int> IncrementViewCountAsync(long productId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "UPDATE \"PRODUCT\" SET \"VIEW_COUNT\" = \"VIEW_COUNT\" + 1, \"UPDATED_AT\" = :updatedAt WHERE \"ID\" = :productId";

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
        const string sql = "SELECT COUNT(*) FROM \"SKU\" WHERE \"PRODUCT_ID\" = :productId";

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

    private static void AddSearchParameters(DbCommand command, ProductQuery query, bool includeStatus)
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
        if (includeStatus && query.Status.HasValue)
        {
            var param = command.CreateParameter();
            param.ParameterName = ":status";
            param.Value = query.Status.Value;
            command.Parameters.Add(param);
        }
    }

    private static void AddUpdateParameters(DbCommand command, Product product)
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

        var updatedAtParam = command.CreateParameter();
        updatedAtParam.ParameterName = ":updatedAt";
        updatedAtParam.Value = product.UpdatedAt;
        command.Parameters.Add(updatedAtParam);
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
            Id = reader.GetInt64(reader.GetOrdinal("ID")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CATEGORY_ID")),
            Name = reader.GetString(reader.GetOrdinal("NAME")),
            Description = reader.IsDBNull(reader.GetOrdinal("DESCRIPTION")) ? null : reader.GetString(reader.GetOrdinal("DESCRIPTION")),
            MainImage = reader.GetString(reader.GetOrdinal("MAIN_IMAGE")),
            Status = reader.GetInt32(reader.GetOrdinal("STATUS")),
            PriceMin = reader.GetDecimal(reader.GetOrdinal("PRICE_MIN")),
            SalesCount = reader.GetInt32(reader.GetOrdinal("SALES_COUNT")),
            ViewCount = reader.GetInt32(reader.GetOrdinal("VIEW_COUNT")),
            AvgRating = reader.GetDecimal(reader.GetOrdinal("AVG_RATING")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT"))
        };
    }

    private static ProductListItemDto MapToListItemDto(DbDataReader reader)
    {
        return new ProductListItemDto(
            ProductId: reader.GetInt64(reader.GetOrdinal("ID")),
            CategoryId: reader.GetInt32(reader.GetOrdinal("CATEGORY_ID")),
            Name: reader.GetString(reader.GetOrdinal("NAME")),
            MainImage: reader.GetString(reader.GetOrdinal("MAIN_IMAGE")),
            PriceMin: reader.GetDecimal(reader.GetOrdinal("PRICE_MIN")),
            SalesCount: reader.GetInt32(reader.GetOrdinal("SALES_COUNT")),
            AvgRating: reader.GetDecimal(reader.GetOrdinal("AVG_RATING")),
            Status: reader.GetInt32(reader.GetOrdinal("STATUS"))
        );
    }
}
