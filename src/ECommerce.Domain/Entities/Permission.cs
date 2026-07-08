namespace ECommerce.Domain.Entities;

/// <summary>
/// PERMISSION 表实体：保存资源路径和操作类型对应的权限点。
/// </summary>
public sealed class Permission
{
    public int Id { get; set; }

    public string PermName { get; set; } = string.Empty;

    public string? ResourcePath { get; set; }

    public string? Action { get; set; }

    public string? Description { get; set; }
}
