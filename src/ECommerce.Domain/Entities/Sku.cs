namespace ECommerce.Domain.Entities;

public class Sku
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string SpecDesc { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int Stock { get; set; }
    public int LockedStock { get; set; }
    public int WarningStock { get; set; }
    public string? SkuImage { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
