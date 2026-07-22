namespace ECommerce.Domain.Entities;

/// <summary>
/// USER_ROLE 表实体：保存用户和角色的多对多绑定关系。
/// </summary>
public sealed class UserRole
{
    public int Id { get; set; }

    public long UserId { get; set; }

    public int RoleId { get; set; }

    public DateTime AssignedAt { get; set; }
}
