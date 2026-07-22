namespace ECommerce.Infrastructure.Data;

public sealed record DatabaseCheckResult(
    bool Connected,
    bool Configured,
    string Database,
    string? SessionUser,
    string? CurrentSchema,
    string? ServiceName,
    string? ServerTime,
    long ElapsedMilliseconds,
    DateTimeOffset CheckedAt,
    string? ErrorMessage);
