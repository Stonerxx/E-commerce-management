namespace ECommerce.Domain.Entities;

public class Cart
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public long SkuId { get; set; }

    public int Quantity { get; set; }

    public int Selected { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
