namespace ECommerce.Domain.Entities;

public sealed class Logistics
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string TrackingNo { get; set; } = string.Empty;

    public DateTime? ShippedAt { get; set; }

    public int Status { get; set; }

    public IReadOnlyList<LogisticsTrack> Tracks { get; set; } = Array.Empty<LogisticsTrack>();
}
