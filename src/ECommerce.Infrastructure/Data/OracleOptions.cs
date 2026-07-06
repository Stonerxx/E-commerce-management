namespace ECommerce.Infrastructure.Data;

public sealed class OracleOptions
{
    public const string SectionName = "Oracle";

    public string ConnectionString { get; init; } = string.Empty;
}
