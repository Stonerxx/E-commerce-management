namespace ECommerce.Domain.Entities;

public class OrderItem
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long SkuId { get; set; }

    public string ProductNameSnap { get; set; } = string.Empty;

    public string SpecSnap { get; set; } = string.Empty;

    public string MainImageSnap { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal Subtotal { get; set; }
}
