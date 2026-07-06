using ECommerce.Application.DTOs;
using ECommerce.Shared.Contracts;

namespace ECommerce.Application.Services;

public interface IUserService
{
    Task<PagedResult<UserDto>> SearchUsersAsync(UserQuery query, CancellationToken cancellationToken = default);

    Task SetUserStatusAsync(long userId, int status, long operatorId, CancellationToken cancellationToken = default);

    Task AssignRolesAsync(long userId, IReadOnlyList<int> roleIds, long operatorId, CancellationToken cancellationToken = default);
}
