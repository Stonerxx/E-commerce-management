namespace ECommerce.Domain.Entities;

public class OrderItem
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long SkuId { get; set; }

    // 查询订单详情时由 SKU 关联得到，不是 ORDER_ITEM 表字段。
    public long ProductId { get; set; }

    public string ProductNameSnap { get; set; } = string.Empty;

    public string SpecSnap { get; set; } = string.Empty;

    public string MainImageSnap { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal Subtotal { get; set; }
}
