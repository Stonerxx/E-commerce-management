namespace ECommerce.Domain.Entities;

/// <summary>
/// ROLE 表实体：保存 USER、SERVICE、ADMIN 等系统角色。
/// </summary>
public sealed class Role
{
    public int Id { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}
