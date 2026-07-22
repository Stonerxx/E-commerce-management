namespace ECommerce.Domain.Entities;

/// <summary>
/// USER 表实体：保存用户账号、密码哈希、状态和登录时间。
/// </summary>
public sealed class User
{
    public long Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? AvatarUrl { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }
}
