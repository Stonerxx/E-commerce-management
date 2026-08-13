using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Shared.Contracts;

namespace ECommerce.Infrastructure.Repositories;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(long reviewId, CancellationToken cancellationToken = default);

    Task<bool> OrderContainsProductAsync(long orderId, long productId, CancellationToken cancellationToken = default);

    Task<bool> HasReviewedAsync(long orderId, long productId, long userId, CancellationToken cancellationToken = default);

    Task<long> InsertAsync(Review review, CancellationToken cancellationToken = default);

    Task<PagedResult<Review>> SearchByProductAsync(long productId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    Task<PagedResult<Review>> SearchAdminAsync(ReviewQuery query, CancellationToken cancellationToken = default);

    Task<bool> UpdateStatusAsync(long reviewId, int status, CancellationToken cancellationToken = default);

    Task RefreshProductAverageRatingAsync(long productId, CancellationToken cancellationToken = default);
}
