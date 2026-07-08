namespace ECommerce.Domain.Entities;

public class OrderMain
{
    public long Id { get; set; }

    public string OrderNo { get; set; } = string.Empty;

    public long UserId { get; set; }

    public long AddressId { get; set; }

    public long? UserCouponId { get; set; }

    public int Status { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal PayAmount { get; set; }

    public DateTime PayExpireTime { get; set; }

    public string ReceiverSnapshot { get; set; } = string.Empty;

    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // 导航属性（便于领域逻辑，非数据库字段）
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderLog> Logs { get; set; } = new List<OrderLog>();
}
