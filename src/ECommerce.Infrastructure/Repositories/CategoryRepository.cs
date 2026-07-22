using System.Data.Common;
using System.Text;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using ECommerce.Shared.Abstractions;

namespace ECommerce.Infrastructure.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(bool includeDisabled, CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(int categoryId, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(Category category, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(Category category, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int categoryId, CancellationToken cancellationToken = default);

    Task<bool> HasChildrenAsync(int categoryId, CancellationToken cancellationToken = default);

    Task<bool> HasProductsAsync(int categoryId, CancellationToken cancellationToken = default);
}

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(bool includeDisabled, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var sql = new StringBuilder();
        sql.Append("SELECT \"ID\", \"PARENT_ID\", \"NAME\", \"TREE_LEVEL\", \"SORT_ORDER\", \"STATUS\", \"ICON_URL\", \"CREATED_AT\" FROM \"CATEGORY\"");
        
        if (!includeDisabled)
        {
            sql.Append(" WHERE \"STATUS\" = 1");
        }
        
        sql.Append(" ORDER BY \"TREE_LEVEL\", \"SORT_ORDER\", \"ID\"");

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql.ToString();

        var categories = new List<Category>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(MapFromReader(reader));
        }

        return categories;
    }

    public async Task<Category?> GetByIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT \"ID\", \"PARENT_ID\", \"NAME\", \"TREE_LEVEL\", \"SORT_ORDER\", \"STATUS\", \"ICON_URL\", \"CREATED_AT\" FROM \"CATEGORY\" WHERE \"ID\" = :categoryId";

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

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapFromReader(reader);
    }

    public async Task<int> CreateAsync(Category category, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO "CATEGORY" ("PARENT_ID", "NAME", "TREE_LEVEL", "SORT_ORDER", "STATUS", "ICON_URL", "CREATED_AT")
            VALUES (:parentId, :name, :treeLevel, :sortOrder, :status, :iconUrl, :createdAt)
            RETURNING "ID" INTO :newId
            """;

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;

        AddParameters(command, category);

        var newIdParam = command.CreateParameter();
        newIdParam.ParameterName = ":newId";
        newIdParam.DbType = System.Data.DbType.Int32;
        newIdParam.Direction = System.Data.ParameterDirection.Output;
        command.Parameters.Add(newIdParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return OracleValueConverter.ToInt32(newIdParam.Value);
    }

    public async Task<int> UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE "CATEGORY" 
            SET "PARENT_ID" = :parentId, "NAME" = :name, "TREE_LEVEL" = :treeLevel, "SORT_ORDER" = :sortOrder, 
                "STATUS" = :status, "ICON_URL" = :iconUrl
            WHERE "ID" = :categoryId
            """;

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;

        AddParameters(command, category, includeCreatedAt: false);

        var idParam = command.CreateParameter();
        idParam.ParameterName = ":categoryId";
        idParam.Value = category.Id;
        command.Parameters.Add(idParam);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> DeleteAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM \"CATEGORY\" WHERE \"ID\" = :categoryId";

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

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> HasChildrenAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(*) FROM \"CATEGORY\" WHERE \"PARENT_ID\" = :categoryId";

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

    public async Task<bool> HasProductsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(*) FROM \"PRODUCT\" WHERE \"CATEGORY_ID\" = :categoryId";

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

    private static void AddParameters(DbCommand command, Category category, bool includeCreatedAt = true)
    {
        var parentIdParam = command.CreateParameter();
        parentIdParam.ParameterName = ":parentId";
        parentIdParam.Value = category.ParentId.HasValue ? (object)category.ParentId.Value : DBNull.Value;
        command.Parameters.Add(parentIdParam);

        var nameParam = command.CreateParameter();
        nameParam.ParameterName = ":name";
        nameParam.Value = category.Name;
        command.Parameters.Add(nameParam);

        var treeLevelParam = command.CreateParameter();
        treeLevelParam.ParameterName = ":treeLevel";
        treeLevelParam.Value = category.TreeLevel;
        command.Parameters.Add(treeLevelParam);

        var sortOrderParam = command.CreateParameter();
        sortOrderParam.ParameterName = ":sortOrder";
        sortOrderParam.Value = category.SortOrder;
        command.Parameters.Add(sortOrderParam);

        var statusParam = command.CreateParameter();
        statusParam.ParameterName = ":status";
        statusParam.Value = category.Status;
        command.Parameters.Add(statusParam);

        var iconUrlParam = command.CreateParameter();
        iconUrlParam.ParameterName = ":iconUrl";
        iconUrlParam.Value = string.IsNullOrEmpty(category.IconUrl) ? DBNull.Value : (object)category.IconUrl;
        command.Parameters.Add(iconUrlParam);

        if (includeCreatedAt)
        {
            var createdAtParam = command.CreateParameter();
            createdAtParam.ParameterName = ":createdAt";
            createdAtParam.Value = category.CreatedAt;
            command.Parameters.Add(createdAtParam);
        }
    }

    private static Category MapFromReader(DbDataReader reader)
    {
        return new Category
        {
            Id = reader.GetInt32(reader.GetOrdinal("ID")),
            ParentId = reader.IsDBNull(reader.GetOrdinal("PARENT_ID")) ? null : reader.GetInt32(reader.GetOrdinal("PARENT_ID")),
            Name = reader.GetString(reader.GetOrdinal("NAME")),
            TreeLevel = reader.GetInt32(reader.GetOrdinal("TREE_LEVEL")),
            SortOrder = reader.GetInt32(reader.GetOrdinal("SORT_ORDER")),
            Status = reader.GetInt32(reader.GetOrdinal("STATUS")),
            IconUrl = reader.IsDBNull(reader.GetOrdinal("ICON_URL")) ? null : reader.GetString(reader.GetOrdinal("ICON_URL")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT"))
        };
    }
}
