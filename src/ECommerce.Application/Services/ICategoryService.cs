using ECommerce.Application.DTOs;

namespace ECommerce.Application.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryTreeDto>> GetTreeAsync(bool includeDisabled, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CategoryRequest request, long operatorId, CancellationToken cancellationToken = default);

    Task UpdateAsync(int categoryId, CategoryRequest request, long operatorId, CancellationToken cancellationToken = default);

    Task DeleteOrDisableAsync(int categoryId, long operatorId, CancellationToken cancellationToken = default);
}
