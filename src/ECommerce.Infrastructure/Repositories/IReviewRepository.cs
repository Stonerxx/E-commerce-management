using ECommerce.Domain.Entities;
using ECommerce.Shared.Contracts;

namespace ECommerce.Infrastructure.Repositories;

public interface IReviewRepository
{
    Task<long> InsertAsync(Review review, CancellationToken cancellationToken = default);
    
    Task<bool> HasReviewedAsync(long orderId, long productId, long userId, CancellationToken cancellationToken = default);
    
    Task<PagedResult<Review>> GetByProductAsync(long productId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    
    Task<PagedResult<Review>> GetForAdminAsync(long? productId, int? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    
    Task<Review?> GetByIdAsync(long reviewId, CancellationToken cancellationToken = default);
    
    Task<bool> UpdateStatusAsync(long reviewId, int status, CancellationToken cancellationToken = default);
}
