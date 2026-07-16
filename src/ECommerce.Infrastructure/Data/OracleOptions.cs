namespace ECommerce.Infrastructure.Data;

public sealed class OracleOptions
{
    public const string SectionName = "Oracle";

    public string ConnectionString { get; init; } = string.Empty;

    public string Host { get; init; } = "120.55.76.207";

    public int Port { get; init; } = 1521;

    public string ServiceName { get; init; } = "FREEPDB1";
}
