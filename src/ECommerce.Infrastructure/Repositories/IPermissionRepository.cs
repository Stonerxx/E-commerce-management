using ECommerce.Application.DTOs;

namespace ECommerce.Infrastructure.Repositories;

public interface IPermissionRepository
{
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<bool> RoleExistsAsync(int roleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(string? keyword, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetExistingPermissionIdsAsync(IReadOnlyList<int> permissionIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RolePermissionDto>> GetRolePermissionsAsync(int roleId, CancellationToken cancellationToken = default);

    Task ReplaceRolePermissionsAsync(int roleId, IReadOnlyList<int> permissionIds, CancellationToken cancellationToken = default);

    Task<bool> PermissionRuleExistsAsync(string requestPath, string httpMethod, CancellationToken cancellationToken = default);

    Task<bool> HasRolePermissionAsync(IReadOnlyList<string> roleNames, string requestPath, string httpMethod, CancellationToken cancellationToken = default);
}
