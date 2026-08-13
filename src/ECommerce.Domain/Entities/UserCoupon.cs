namespace ECommerce.Domain.Entities;

public sealed class UserCoupon
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public int CouponTemplateId { get; set; }

    public int Status { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public long? OrderId { get; set; }
}
