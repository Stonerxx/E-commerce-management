namespace ECommerce.Domain.Entities;

/// <summary>
/// ROLE_PERMISSION 表实体：保存角色和权限的多对多绑定关系。
/// </summary>
public sealed class RolePermission
{
    public int Id { get; set; }

    public int RoleId { get; set; }

    public int PermissionId { get; set; }

    public DateTime CreatedAt { get; set; }
}
