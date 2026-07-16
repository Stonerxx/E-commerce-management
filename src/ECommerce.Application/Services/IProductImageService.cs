using ECommerce.Application.DTOs;

namespace ECommerce.Application.Services;

public interface IProductImageService
{
    Task<long> AddAsync(long productId, ProductImageRequest request, long operatorId, CancellationToken cancellationToken = default);

    Task DeleteAsync(long imageId, long operatorId, CancellationToken cancellationToken = default);
}
