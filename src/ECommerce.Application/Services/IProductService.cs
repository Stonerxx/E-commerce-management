using ECommerce.Application.DTOs;
using ECommerce.Shared.Contracts;

namespace ECommerce.Application.Services;

public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> SearchAsync(ProductQuery query, CancellationToken cancellationToken = default);

    Task<ProductDetailDto> GetDetailAsync(long productId, CancellationToken cancellationToken = default);

    Task<ProductDetailDto> GetPublicDetailAndTrackAsync(long productId, CancellationToken cancellationToken = default);

    Task<long> CreateAsync(ProductSaveRequest request, long operatorId, CancellationToken cancellationToken = default);

    Task UpdateAsync(long productId, ProductSaveRequest request, long operatorId, CancellationToken cancellationToken = default);

    Task SetStatusAsync(long productId, int status, long operatorId, CancellationToken cancellationToken = default);
}
