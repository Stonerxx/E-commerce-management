namespace ECommerce.Domain.Entities;

public class OrderLog
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public int? FromStatus { get; set; }

    public int ToStatus { get; set; }

    public long? OperatorId { get; set; }

    public string? OperatorName { get; set; }

    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; }
}
