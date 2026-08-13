namespace ECommerce.Domain.Entities;

public sealed class LogisticsTrack
{
    public long Id { get; set; }

    public long LogisticsId { get; set; }

    public string TrackDesc { get; set; } = string.Empty;

    public DateTime TrackTime { get; set; }

    public string? Location { get; set; }
}
