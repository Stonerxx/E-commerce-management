namespace ECommerce.Domain.Entities;

public sealed class Payment
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public string PayMethod { get; set; } = string.Empty;

    public int Status { get; set; }

    public string? TradeNo { get; set; }

    public decimal PayAmount { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? CallbackData { get; set; }
}
