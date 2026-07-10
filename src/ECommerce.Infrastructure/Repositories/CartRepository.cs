using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Models;
using ECommerce.Shared.Abstractions;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;

namespace ECommerce.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public CartRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private DbConnection Connection => _unitOfWork.CurrentConnection
        ?? throw new InvalidOperationException("Connection not opened. Call GetOpenConnectionAsync first.");
    private DbTransaction? Transaction => _unitOfWork.CurrentTransaction;

    public async Task<IReadOnlyList<CartItemWithDetails>> GetUserCartWithDetailsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT 
                c.id AS CartItemId,
                c.sku_id AS SkuId,
                p.id AS ProductId,
                p.name AS ProductName,
                s.spec_desc AS SpecDescJson,
                COALESCE(s.sku_image, p.main_image) AS MainImage,
                s.price AS UnitPrice,
                c.quantity AS Quantity,
                c.selected AS Selected,
                c.updated_at AS UpdatedAt
            FROM cart c
            JOIN sku s ON c.sku_id = s.id
            JOIN product p ON s.product_id = p.id
            WHERE c.user_id = :UserId
            ORDER BY c.updated_at DESC";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        var pUserId = cmd.CreateParameter();
        pUserId.ParameterName = "UserId";
        pUserId.Value = userId;
        cmd.Parameters.Add(pUserId);

        var result = new List<CartItemWithDetails>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new CartItemWithDetails
            {
                CartItemId = reader.GetInt64(reader.GetOrdinal("CartItemId")),
                SkuId = reader.GetInt64(reader.GetOrdinal("SkuId")),
                ProductId = reader.GetInt64(reader.GetOrdinal("ProductId")),
                ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                SpecDescJson = reader.GetString(reader.GetOrdinal("SpecDescJson")),
                MainImage = reader.GetString(reader.GetOrdinal("MainImage")),
                UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                Selected = reader.GetInt32(reader.GetOrdinal("Selected")) == 1,
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            });
        }
        return result;
    }

    public async Task<Cart?> GetByUserAndSkuAsync(long userId, long skuId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM cart WHERE user_id = :UserId AND sku_id = :SkuId";
        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        var p1 = cmd.CreateParameter();
        p1.ParameterName = "UserId";
        p1.Value = userId;
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.ParameterName = "SkuId";
        p2.Value = skuId;
        cmd.Parameters.Add(p2);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapCart(reader);
        }
        return null;
    }

    public async Task<IReadOnlyList<Cart>> GetByIdsAsync(IReadOnlyList<long> cartItemIds, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        if (cartItemIds == null || cartItemIds.Count == 0)
            return Array.Empty<Cart>();

        // 构建 IN 查询，参数数量动态
        var paramNames = string.Join(", ", cartItemIds.Select((_, i) => $":Id{i}"));
        var sql = $"SELECT * FROM cart WHERE id IN ({paramNames})";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        for (int i = 0; i < cartItemIds.Count; i++)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = $"Id{i}";
            p.Value = cartItemIds[i];
            cmd.Parameters.Add(p);
        }

        var result = new List<Cart>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapCart(reader));
        }
        return result;
    }

    public async Task AddAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO cart (user_id, sku_id, quantity, selected, created_at, updated_at)
            VALUES (:UserId, :SkuId, :Quantity, :Selected, :CreatedAt, :UpdatedAt)
            RETURNING id INTO :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        cmd.Parameters.Add(CreateParameter("UserId", cart.UserId));
        cmd.Parameters.Add(CreateParameter("SkuId", cart.SkuId));
        cmd.Parameters.Add(CreateParameter("Quantity", cart.Quantity));
        cmd.Parameters.Add(CreateParameter("Selected", cart.Selected));
        cmd.Parameters.Add(CreateParameter("CreatedAt", cart.CreatedAt));
        cmd.Parameters.Add(CreateParameter("UpdatedAt", cart.UpdatedAt));

        var pId = cmd.CreateParameter();
        pId.ParameterName = "Id";
        pId.DbType = DbType.Int64;
        pId.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(pId);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        cart.Id = Convert.ToInt64(pId.Value);
    }

    public async Task UpdateAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE cart 
            SET quantity = :Quantity, selected = :Selected, updated_at = :UpdatedAt
            WHERE id = :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        cmd.Parameters.Add(CreateParameter("Quantity", cart.Quantity));
        cmd.Parameters.Add(CreateParameter("Selected", cart.Selected));
        cmd.Parameters.Add(CreateParameter("UpdatedAt", cart.UpdatedAt));
        cmd.Parameters.Add(CreateParameter("Id", cart.Id));

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RemoveAsync(long cartItemId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM cart WHERE id = :Id";
        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("Id", cartItemId));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ClearSelectedAsync(long userId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM cart WHERE user_id = :UserId AND selected = 1";
        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("UserId", userId));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ClearByIdsAsync(long userId, IReadOnlyList<long> cartItemIds, CancellationToken cancellationToken = default)
    {
        if (cartItemIds.Count == 0)
        {
            return;
        }

        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var itemIds = cartItemIds.Distinct().ToArray();
        var parameterNames = string.Join(", ", itemIds.Select((_, index) => $":CartItemId{index}"));
        var sql = $"DELETE FROM cart WHERE user_id = :UserId AND id IN ({parameterNames})";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("UserId", userId));
        for (var index = 0; index < itemIds.Length; index++)
        {
            cmd.Parameters.Add(CreateParameter($"CartItemId{index}", itemIds[index]));
        }

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ClearAllAsync(long userId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM cart WHERE user_id = :UserId";
        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("UserId", userId));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    // ---------- 辅助方法 ----------
    private static Cart MapCart(DbDataReader reader)
    {
        return new Cart
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
            SkuId = reader.GetInt64(reader.GetOrdinal("sku_id")),
            Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
            Selected = reader.GetInt32(reader.GetOrdinal("selected")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }

    private static DbParameter CreateParameter(string name, object? value)
    {
        var p = new OracleParameter(name, value ?? DBNull.Value);
        return p;
    }
}
