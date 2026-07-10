namespace ECommerce.Application.DTOs;

public sealed record RoleDto(
    int RoleId,
    string RoleName,
    string? Description,
    DateTime CreatedAt);

public sealed record PermissionDto(
    int PermissionId,
    string PermName,
    string? ResourcePath,
    string? Action,
    string? Description);

public sealed record RolePermissionDto(
    int RoleId,
    string RoleName,
    int PermissionId,
    string PermName,
    string? ResourcePath,
    string? Action);

public sealed record BindRolePermissionsRequest(IReadOnlyList<int> PermissionIds);
