namespace ECommerce.Domain.Entities;

/// <summary>
/// OPERATION_LOG 表实体：保存后台关键写操作审计记录。
/// </summary>
public sealed class OperationLog
{
    public long Id { get; set; }

    public long OperatorId { get; set; }

    public string OperatorName { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public string? RequestParams { get; set; }

    public int Result { get; set; }

    public DateTime CreatedAt { get; set; }
}
