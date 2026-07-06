namespace ECommerce.Application.DTOs;

public sealed record ShipmentRequest(
    string CompanyName,
    string TrackingNo,
    DateTime? ShippedAt);

public sealed record LogisticsTrackRequest(
    string TrackDesc,
    DateTime TrackTime,
    string? Location);

public sealed record LogisticsTrackDto(
    long TrackId,
    string TrackDesc,
    DateTime TrackTime,
    string? Location);

public sealed record LogisticsDto(
    long LogisticsId,
    long OrderId,
    string CompanyName,
    string TrackingNo,
    DateTime? ShippedAt,
    int Status,
    IReadOnlyList<LogisticsTrackDto> Tracks);
