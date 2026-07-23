using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Shared.Contracts;

namespace ECommerce.Infrastructure.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long userId, CancellationToken cancellationToken = default);

    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<long> CreateUserWithDefaultRoleAsync(User user, string defaultRoleName, CancellationToken cancellationToken = default);

    Task UpdateLastLoginAsync(long userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetRoleNamesAsync(long userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetExistingRoleIdsAsync(IReadOnlyList<int> roleIds, CancellationToken cancellationToken = default);

    Task<int?> GetRoleIdByNameAsync(string roleName, CancellationToken cancellationToken = default);

    Task<int> CountActiveUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default);

    Task LockRoleAsync(string roleName, CancellationToken cancellationToken = default);

    Task<PagedResult<UserDto>> SearchUsersAsync(UserQuery query, CancellationToken cancellationToken = default);

    Task SetUserStatusAsync(long userId, int status, CancellationToken cancellationToken = default);

    Task ReplaceUserRolesAsync(long userId, IReadOnlyList<int> roleIds, CancellationToken cancellationToken = default);
}
