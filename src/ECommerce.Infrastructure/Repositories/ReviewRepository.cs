using ECommerce.Domain.Entities;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;
using System.Text;

namespace ECommerce.Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public ReviewRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private DbConnection Connection => _unitOfWork.CurrentConnection ?? throw new InvalidOperationException("Connection not opened. Call GetOpenConnectionAsync first.");
    private DbTransaction? Transaction => _unitOfWork.CurrentTransaction;

    public async Task<long> InsertAsync(Review review, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO review 
                (order_id, product_id, user_id, rating, content, images, is_anonymous, status)
            VALUES 
                (:OrderId, :ProductId, :UserId, :Rating, :Content, :Images, :IsAnonymous, :Status)
            RETURNING id INTO :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        cmd.Parameters.Add(CreateParameter("OrderId", review.OrderId));
        cmd.Parameters.Add(CreateParameter("ProductId", review.ProductId));
        cmd.Parameters.Add(CreateParameter("UserId", review.UserId));
        cmd.Parameters.Add(CreateParameter("Rating", review.Rating));
        cmd.Parameters.Add(CreateParameter("Content", review.Content));
        cmd.Parameters.Add(CreateParameter("Images", review.Images));
        cmd.Parameters.Add(CreateParameter("IsAnonymous", review.IsAnonymous));
        cmd.Parameters.Add(CreateParameter("Status", review.Status));

        var pId = cmd.CreateParameter();
        pId.ParameterName = "Id";
        pId.DbType = DbType.Int64;
        pId.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(pId);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        // 防止数值类型被误传成decimal
        review.Id = Convert.ToInt64(pId.Value);
        return review.Id;
    }

    public async Task<bool> HasReviewedAsync(long orderId, long productId, long userId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT COUNT(1) FROM review 
            WHERE order_id = :OrderId AND product_id = :ProductId AND user_id = :UserId";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        
        cmd.Parameters.Add(CreateParameter("OrderId", orderId));
        cmd.Parameters.Add(CreateParameter("ProductId", productId));
        cmd.Parameters.Add(CreateParameter("UserId", userId));

        var count = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }

    public async Task<PagedResult<Review>> GetByProductAsync(long productId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string countSql = "SELECT COUNT(1) FROM review WHERE product_id = :ProductId AND status = 1";
        
        await using var cmdCount = Connection.CreateCommand();
        cmdCount.CommandText = countSql;
        cmdCount.Transaction = Transaction;
        cmdCount.Parameters.Add(CreateParameter("ProductId", productId));

        long totalCount = Convert.ToInt64(await cmdCount.ExecuteScalarAsync(cancellationToken));
        if (totalCount == 0)
        {
            return PagedResult<Review>.Empty(pageIndex, pageSize);
        }

        int offset = (pageIndex - 1) * pageSize;
        const string dataSql = @"
            SELECT id, order_id, product_id, user_id, rating, content, images, is_anonymous, status, created_at
            FROM review 
            WHERE product_id = :ProductId AND status = 1
            ORDER BY created_at DESC
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY";

        await using var cmdData = Connection.CreateCommand();
        cmdData.CommandText = dataSql;
        cmdData.Transaction = Transaction;
        cmdData.Parameters.Add(CreateParameter("ProductId", productId));
        cmdData.Parameters.Add(CreateParameter("Offset", offset));
        cmdData.Parameters.Add(CreateParameter("PageSize", pageSize));

        var items = new List<Review>();
        await using var reader = await cmdData.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapReview(reader));
        }

        return new PagedResult<Review>(items, pageIndex, pageSize, totalCount);
    }

    public async Task<PagedResult<Review>> GetForAdminAsync(long? productId, int? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        
        var whereBuilder = new StringBuilder("WHERE 1=1");
        var parameters = new List<DbParameter>();

        if (productId.HasValue)
        {
            whereBuilder.Append(" AND product_id = :ProductId");
            parameters.Add(CreateParameter("ProductId", productId.Value));
        }

        if (status.HasValue)
        {
            whereBuilder.Append(" AND status = :Status");
            parameters.Add(CreateParameter("Status", status.Value));
        }

        string where = whereBuilder.ToString();

        string countSql = $"SELECT COUNT(1) FROM review {where}";
        await using var cmdCount = Connection.CreateCommand();
        cmdCount.CommandText = countSql;
        cmdCount.Transaction = Transaction;
        foreach (var p in parameters) cmdCount.Parameters.Add(p);

        long totalCount = Convert.ToInt64(await cmdCount.ExecuteScalarAsync(cancellationToken));
        if (totalCount == 0)
        {
            return PagedResult<Review>.Empty(pageIndex, pageSize);
        }

        int offset = (pageIndex - 1) * pageSize;
        string dataSql = $@"
            SELECT id, order_id, product_id, user_id, rating, content, images, is_anonymous, status, created_at
            FROM review 
            {where}
            ORDER BY created_at DESC
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY";

        var dataParams = new List<DbParameter>(parameters);
        dataParams.Add(CreateParameter("Offset", offset));
        dataParams.Add(CreateParameter("PageSize", pageSize));

        await using var cmdData = Connection.CreateCommand();
        cmdData.CommandText = dataSql;
        cmdData.Transaction = Transaction;
        foreach (var p in dataParams) cmdData.Parameters.Add(p);

        var items = new List<Review>();
        await using var reader = await cmdData.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapReview(reader));
        }

        return new PagedResult<Review>(items, pageIndex, pageSize, totalCount);
    }

    public async Task<Review?> GetByIdAsync(long reviewId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, order_id, product_id, user_id, rating, content, images, is_anonymous, status, created_at
            FROM review 
            WHERE id = :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("Id", reviewId));

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapReview(reader);
        }
        return null;
    }

    public async Task<bool> UpdateStatusAsync(long reviewId, int status, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"UPDATE review SET status = :Status WHERE id = :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        cmd.Parameters.Add(CreateParameter("Status", status));
        cmd.Parameters.Add(CreateParameter("Id", reviewId));

        return await cmd.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static Review MapReview(DbDataReader reader)
    {
        return new Review
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            OrderId = reader.GetInt64(reader.GetOrdinal("order_id")),
            ProductId = reader.GetInt64(reader.GetOrdinal("product_id")),
            UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
            Rating = reader.GetInt32(reader.GetOrdinal("rating")),
            Content = reader.IsDBNull(reader.GetOrdinal("content")) ? null : reader.GetString(reader.GetOrdinal("content")),
            Images = reader.IsDBNull(reader.GetOrdinal("images")) ? null : reader.GetString(reader.GetOrdinal("images")),
            IsAnonymous = reader.GetInt32(reader.GetOrdinal("is_anonymous")),
            Status = reader.GetInt32(reader.GetOrdinal("status")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }

    private static DbParameter CreateParameter(string name, object? value)
    {
        return new OracleParameter(name, value ?? DBNull.Value);
    }
}
