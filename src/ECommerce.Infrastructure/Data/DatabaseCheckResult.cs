namespace ECommerce.Infrastructure.Data;

public sealed record DatabaseCheckResult(
    bool Connected,
    string Database,
    string? ServerTime,
    string? ErrorMessage);
