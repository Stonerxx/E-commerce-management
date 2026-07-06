namespace ECommerce.Infrastructure.Data;

public sealed class OracleOptions
{
    public const string SectionName = "Oracle";

    public string ConnectionString { get; init; } = string.Empty;

    public int HealthCheckTimeoutSeconds { get; init; } = 5;

    public bool HasUsableConnectionString =>
        !string.IsNullOrWhiteSpace(ConnectionString)
        && !ConnectionString.Contains("change_me", StringComparison.OrdinalIgnoreCase);
}
