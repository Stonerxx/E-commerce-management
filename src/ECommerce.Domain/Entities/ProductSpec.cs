namespace ECommerce.Domain.Entities;

public class ProductSpec
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string SpecName { get; set; } = string.Empty;
    public string SpecValue { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
