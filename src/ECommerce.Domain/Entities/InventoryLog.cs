namespace ECommerce.Domain.Entities;

public class InventoryLog
{
    public long Id { get; set; }
    public long SkuId { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public int ChangeQty { get; set; }
    public int BeforeStock { get; set; }
    public int AfterStock { get; set; }
    public long? OperatorId { get; set; }
    public long? RefOrderId { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
}
