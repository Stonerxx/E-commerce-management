using System.Data.Common;
using ECommerce.Domain.Entities;
using ECommerce.Shared.Abstractions;

namespace ECommerce.Infrastructure.Repositories;

public interface IProductSpecRepository
{
    Task<IReadOnlyList<ProductSpec>> GetByProductAsync(long productId, CancellationToken cancellationToken = default);

    Task<long> CreateAsync(ProductSpec spec, CancellationToken cancellationToken = default);

    Task<int> DeleteByProductAsync(long productId, CancellationToken cancellationToken = default);
}

public sealed class ProductSpecRepository : IProductSpecRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductSpecRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ProductSpec>> GetByProductAsync(long productId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT \"ID\", \"PRODUCT_ID\", \"SPEC_NAME\", \"SPEC_VALUE\", \"SORT_ORDER\", \"CREATED_AT\" FROM \"PRODUCT_SPEC\" WHERE \"PRODUCT_ID\" = :productId ORDER BY \"SORT_ORDER\"";

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

        var specs = new List<ProductSpec>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            specs.Add(MapFromReader(reader));
        }

        return specs;
    }

    public async Task<long> CreateAsync(ProductSpec spec, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO "PRODUCT_SPEC" ("PRODUCT_ID", "SPEC_NAME", "SPEC_VALUE", "SORT_ORDER", "CREATED_AT")
            VALUES (:productId, :specName, :specValue, :sortOrder, :createdAt)
            RETURNING "ID" INTO :newId
            """;

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;

        AddParameters(command, spec);

        var newIdParam = command.CreateParameter();
        newIdParam.ParameterName = ":newId";
        newIdParam.DbType = System.Data.DbType.Int64;
        newIdParam.Direction = System.Data.ParameterDirection.Output;
        command.Parameters.Add(newIdParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return Convert.ToInt64(newIdParam.Value);
    }

    public async Task<int> DeleteByProductAsync(long productId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM \"PRODUCT_SPEC\" WHERE \"PRODUCT_ID\" = :productId";

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

    private static void AddParameters(DbCommand command, ProductSpec spec)
    {
        var productIdParam = command.CreateParameter();
        productIdParam.ParameterName = ":productId";
        productIdParam.Value = spec.ProductId;
        command.Parameters.Add(productIdParam);

        var specNameParam = command.CreateParameter();
        specNameParam.ParameterName = ":specName";
        specNameParam.Value = spec.SpecName;
        command.Parameters.Add(specNameParam);

        var specValueParam = command.CreateParameter();
        specValueParam.ParameterName = ":specValue";
        specValueParam.Value = spec.SpecValue;
        command.Parameters.Add(specValueParam);

        var sortOrderParam = command.CreateParameter();
        sortOrderParam.ParameterName = ":sortOrder";
        sortOrderParam.Value = spec.SortOrder;
        command.Parameters.Add(sortOrderParam);

        var createdAtParam = command.CreateParameter();
        createdAtParam.ParameterName = ":createdAt";
        createdAtParam.Value = spec.CreatedAt;
        command.Parameters.Add(createdAtParam);
    }

    private static ProductSpec MapFromReader(DbDataReader reader)
    {
        return new ProductSpec
        {
            Id = reader.GetInt64(reader.GetOrdinal("ID")),
            ProductId = reader.GetInt64(reader.GetOrdinal("PRODUCT_ID")),
            SpecName = reader.GetString(reader.GetOrdinal("SPEC_NAME")),
            SpecValue = reader.GetString(reader.GetOrdinal("SPEC_VALUE")),
            SortOrder = reader.GetInt32(reader.GetOrdinal("SORT_ORDER")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT"))
        };
    }
}