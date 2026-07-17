using ECommerce.Shared.Contracts;

namespace ECommerce.Application.DTOs;

public sealed record ReviewRequest(
    long OrderId,
    long ProductId,
    int Rating,
    string? Content,
    IReadOnlyList<string> Images,
    bool IsAnonymous);

public sealed record ReviewQuery : PageQuery
{
    public long? ProductId { get; init; }

    public int? Status { get; init; }
}

public sealed record ReviewDto(
    long ReviewId,
    long OrderId,
    long ProductId,
    long? UserId,
    int Rating,
    string? Content,
    IReadOnlyList<string> Images,
    bool IsAnonymous,
    int Status,
    DateTime CreatedAt);
