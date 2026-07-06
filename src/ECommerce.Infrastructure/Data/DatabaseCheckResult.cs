namespace ECommerce.Infrastructure.Data;

public sealed record DatabaseCheckResult(
    bool Connected,
    bool Configured,
    string Database,
    string? ServerTime,
    long ElapsedMilliseconds,
    DateTimeOffset CheckedAt,
    string? ErrorMessage);
