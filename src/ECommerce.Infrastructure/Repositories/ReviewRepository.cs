using System.Data;
using System.Data.Common;
using System.Text;
using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Infrastructure.Repositories;

public sealed class ReviewRepository : IReviewRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public ReviewRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private DbConnection Connection => _unitOfWork.CurrentConnection
        ?? throw new InvalidOperationException("Connection not opened. Call GetOpenConnectionAsync first.");

    private DbTransaction? Transaction => _unitOfWork.CurrentTransaction;

    public async Task<Review?> GetByIdAsync(long reviewId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM review WHERE id = :ReviewId";

        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("ReviewId", reviewId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapReview(reader) : null;
    }

    public async Task<bool> OrderContainsProductAsync(
        long orderId,
        long productId,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT COUNT(1)
            FROM order_item oi
            INNER JOIN sku s ON s.id = oi.sku_id
            WHERE oi.order_id = :OrderId
              AND s.product_id = :ProductId";

        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("OrderId", orderId));
        command.Parameters.Add(CreateParameter("ProductId", productId));
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<bool> HasReviewedAsync(
        long orderId,
        long productId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT COUNT(1)
            FROM review
            WHERE order_id = :OrderId
              AND product_id = :ProductId
              AND user_id = :UserId";

        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("OrderId", orderId));
        command.Parameters.Add(CreateParameter("ProductId", productId));
        command.Parameters.Add(CreateParameter("UserId", userId));
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<long> InsertAsync(Review review, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO review
                (order_id, product_id, user_id, rating, content, images, is_anonymous, status, created_at)
            VALUES
                (:OrderId, :ProductId, :UserId, :Rating, :Content, :Images, :IsAnonymous, :Status, :CreatedAt)
            RETURNING id INTO :Id";

        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("OrderId", review.OrderId));
        command.Parameters.Add(CreateParameter("ProductId", review.ProductId));
        command.Parameters.Add(CreateParameter("UserId", review.UserId));
        command.Parameters.Add(CreateParameter("Rating", review.Rating));
        command.Parameters.Add(CreateParameter("Content", review.Content));
        command.Parameters.Add(CreateParameter("Images", review.Images));
        command.Parameters.Add(CreateParameter("IsAnonymous", review.IsAnonymous ? 1 : 0));
        command.Parameters.Add(CreateParameter("Status", review.Status));
        command.Parameters.Add(CreateParameter("CreatedAt", review.CreatedAt));

        var idParameter = command.CreateParameter();
        idParameter.ParameterName = "Id";
        idParameter.DbType = DbType.Int64;
        idParameter.Direction = ParameterDirection.Output;
        command.Parameters.Add(idParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
        review.Id = Convert.ToInt64(idParameter.Value);
        return review.Id;
    }

    public Task<PagedResult<Review>> SearchByProductAsync(
        long productId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        const string where = "WHERE product_id = :ProductId AND status = :Status";
        return SearchAsync(
            where,
            command =>
            {
                command.Parameters.Add(CreateParameter("ProductId", productId));
                command.Parameters.Add(CreateParameter("Status", (int)ReviewStatus.Published));
            },
            pageIndex,
            pageSize,
            cancellationToken);
    }

    public Task<PagedResult<Review>> SearchAdminAsync(
        ReviewQuery query,
        CancellationToken cancellationToken = default)
    {
        var where = new StringBuilder("WHERE 1 = 1");
        if (query.ProductId.HasValue)
        {
            where.Append(" AND product_id = :ProductId");
        }

        if (query.Status.HasValue)
        {
            where.Append(" AND status = :Status");
        }

        return SearchAsync(
            where.ToString(),
            command =>
            {
                if (query.ProductId.HasValue)
                {
                    command.Parameters.Add(CreateParameter("ProductId", query.ProductId.Value));
                }

                if (query.Status.HasValue)
                {
                    command.Parameters.Add(CreateParameter("Status", query.Status.Value));
                }
            },
            query.SafePageIndex,
            query.SafePageSize,
            cancellationToken);
    }

    public async Task<bool> UpdateStatusAsync(long reviewId, int status, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = "UPDATE review SET status = :Status WHERE id = :ReviewId";

        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("Status", status));
        command.Parameters.Add(CreateParameter("ReviewId", reviewId));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task RefreshProductAverageRatingAsync(long productId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE product
            SET avg_rating = NVL((
                    SELECT ROUND(AVG(rating), 2)
                    FROM review
                    WHERE product_id = :ProductId
                      AND status = :PublishedStatus
                ), 0),
                updated_at = :UpdatedAt
            WHERE id = :ProductId";

        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("ProductId", productId));
        command.Parameters.Add(CreateParameter("PublishedStatus", (int)ReviewStatus.Published));
        command.Parameters.Add(CreateParameter("UpdatedAt", DateTime.Now));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<PagedResult<Review>> SearchAsync(
        string where,
        Action<DbCommand> addFilterParameters,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);

        await using var countCommand = CreateCommand($"SELECT COUNT(1) FROM review {where}");
        addFilterParameters(countCommand);
        var totalCount = Convert.ToInt64(await countCommand.ExecuteScalarAsync(cancellationToken));
        if (totalCount == 0)
        {
            return PagedResult<Review>.Empty(pageIndex, pageSize);
        }

        var sql = $@"
            SELECT *
            FROM review
            {where}
            ORDER BY created_at DESC, id DESC
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY";
        await using var dataCommand = CreateCommand(sql);
        addFilterParameters(dataCommand);
        dataCommand.Parameters.Add(CreateParameter("Offset", (pageIndex - 1) * pageSize));
        dataCommand.Parameters.Add(CreateParameter("PageSize", pageSize));

        var items = new List<Review>();
        await using var reader = await dataCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapReview(reader));
        }

        return new PagedResult<Review>(items, pageIndex, pageSize, totalCount);
    }

    private DbCommand CreateCommand(string sql)
    {
        var command = Connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = Transaction;
        return command;
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
            IsAnonymous = reader.GetInt32(reader.GetOrdinal("is_anonymous")) == 1,
            Status = reader.GetInt32(reader.GetOrdinal("status")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }

    private static DbParameter CreateParameter(string name, object? value)
    {
        return new OracleParameter(name, value ?? DBNull.Value);
    }
}
