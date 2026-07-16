using System.Data.Common;
using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Shared.Abstractions;

namespace ECommerce.Infrastructure.Repositories;

public interface IProductImageRepository
{
    Task<IReadOnlyList<ProductImageDto>> GetByProductAsync(long productId, CancellationToken cancellationToken = default);

    Task<long> CreateAsync(ProductImage image, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(long imageId, CancellationToken cancellationToken = default);

    Task<int> DeleteByProductAsync(long productId, CancellationToken cancellationToken = default);
}

public sealed class ProductImageRepository : IProductImageRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductImageRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ProductImageDto>> GetByProductAsync(long productId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT \"ID\", \"IMAGE_URL\", \"SORT_ORDER\" FROM \"PRODUCT_IMAGE\" WHERE \"PRODUCT_ID\" = :productId ORDER BY \"SORT_ORDER\"";

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

        var images = new List<ProductImageDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            images.Add(MapToDto(reader));
        }

        return images;
    }

    public async Task<long> CreateAsync(ProductImage image, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO "PRODUCT_IMAGE" ("PRODUCT_ID", "IMAGE_URL", "SORT_ORDER", "CREATED_AT")
            VALUES (:productId, :imageUrl, :sortOrder, :createdAt)
            RETURNING "ID" INTO :newId
            """;

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;

        AddParameters(command, image);

        var newIdParam = command.CreateParameter();
        newIdParam.ParameterName = ":newId";
        newIdParam.DbType = System.Data.DbType.Int64;
        newIdParam.Direction = System.Data.ParameterDirection.Output;
        command.Parameters.Add(newIdParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return Convert.ToInt64(newIdParam.Value);
    }

    public async Task<int> DeleteAsync(long imageId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM \"PRODUCT_IMAGE\" WHERE \"ID\" = :imageId";

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = ":imageId";
        parameter.Value = imageId;
        command.Parameters.Add(parameter);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> DeleteByProductAsync(long productId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM \"PRODUCT_IMAGE\" WHERE \"PRODUCT_ID\" = :productId";

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

    private static void AddParameters(DbCommand command, ProductImage image)
    {
        var productIdParam = command.CreateParameter();
        productIdParam.ParameterName = ":productId";
        productIdParam.Value = image.ProductId;
        command.Parameters.Add(productIdParam);

        var imageUrlParam = command.CreateParameter();
        imageUrlParam.ParameterName = ":imageUrl";
        imageUrlParam.Value = image.ImageUrl;
        command.Parameters.Add(imageUrlParam);

        var sortOrderParam = command.CreateParameter();
        sortOrderParam.ParameterName = ":sortOrder";
        sortOrderParam.Value = image.SortOrder;
        command.Parameters.Add(sortOrderParam);

        var createdAtParam = command.CreateParameter();
        createdAtParam.ParameterName = ":createdAt";
        createdAtParam.Value = image.CreatedAt;
        command.Parameters.Add(createdAtParam);
    }

    private static ProductImageDto MapToDto(DbDataReader reader)
    {
        return new ProductImageDto(
            ImageId: reader.GetInt64(reader.GetOrdinal("ID")),
            ImageUrl: reader.GetString(reader.GetOrdinal("IMAGE_URL")),
            SortOrder: reader.GetInt32(reader.GetOrdinal("SORT_ORDER"))
        );
    }
}