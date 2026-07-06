using ECommerce.Application.DTOs;
using ECommerce.Shared.Contracts;

namespace ECommerce.Application.Services;

public interface IReviewService
{
    Task<long> CreateAsync(long userId, ReviewRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<ReviewDto>> SearchByProductAsync(long productId, PageQuery query, CancellationToken cancellationToken = default);

    Task<PagedResult<ReviewDto>> SearchAdminAsync(ReviewQuery query, CancellationToken cancellationToken = default);

    Task SetStatusAsync(long reviewId, int status, long operatorId, CancellationToken cancellationToken = default);
}
