using ECommerce.Application.DTOs;

namespace ECommerce.Application.Services;

public interface IPermissionService
{
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(string? keyword, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RolePermissionDto>> GetRolePermissionsAsync(int roleId, CancellationToken cancellationToken = default);

    Task BindRolePermissionsAsync(int roleId, IReadOnlyList<int> permissionIds, CancellationToken cancellationToken = default);

    Task<bool> CanAccessAsync(IReadOnlyList<string> roleNames, string requestPath, string httpMethod, CancellationToken cancellationToken = default);
}
