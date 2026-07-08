using System.Data.Common;
using System.Text;
using ECommerce.Domain.Entities;
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
        sql.Append("SELECT id, parent_id, name, tree_level, sort_order, status, icon_url, created_at FROM \"CATEGORY\"");
        
        if (!includeDisabled)
        {
            sql.Append(" WHERE status = 1");
        }
        
        sql.Append(" ORDER BY tree_level, sort_order, id");

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
        const string sql = "SELECT id, parent_id, name, tree_level, sort_order, status, icon_url, created_at FROM \"CATEGORY\" WHERE id = :categoryId";

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
            INSERT INTO "CATEGORY" (parent_id, name, tree_level, sort_order, status, icon_url, created_at)
            VALUES (:parentId, :name, :treeLevel, :sortOrder, :status, :iconUrl, :createdAt)
            RETURNING id INTO :newId
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
        return Convert.ToInt32(newIdParam.Value);
    }

    public async Task<int> UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE "CATEGORY" 
            SET parent_id = :parentId, name = :name, tree_level = :treeLevel, sort_order = :sortOrder, 
                status = :status, icon_url = :iconUrl, created_at = :createdAt
            WHERE id = :categoryId
            """;

        using var command = connection.CreateCommand();
        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }
        command.CommandText = sql;

        AddParameters(command, category);

        var idParam = command.CreateParameter();
        idParam.ParameterName = ":categoryId";
        idParam.Value = category.Id;
        command.Parameters.Add(idParam);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> DeleteAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM \"CATEGORY\" WHERE id = :categoryId";

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
        const string sql = "SELECT COUNT(*) FROM \"CATEGORY\" WHERE parent_id = :categoryId";

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
        const string sql = "SELECT COUNT(*) FROM PRODUCT WHERE category_id = :categoryId";

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

    private static void AddParameters(DbCommand command, Category category)
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

        var createdAtParam = command.CreateParameter();
        createdAtParam.ParameterName = ":createdAt";
        createdAtParam.Value = category.CreatedAt;
        command.Parameters.Add(createdAtParam);
    }

    private static Category MapFromReader(DbDataReader reader)
    {
        return new Category
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            ParentId = reader.IsDBNull(reader.GetOrdinal("parent_id")) ? null : reader.GetInt32(reader.GetOrdinal("parent_id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            TreeLevel = reader.GetInt32(reader.GetOrdinal("tree_level")),
            SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order")),
            Status = reader.GetInt32(reader.GetOrdinal("status")),
            IconUrl = reader.IsDBNull(reader.GetOrdinal("icon_url")) ? null : reader.GetString(reader.GetOrdinal("icon_url")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }
}