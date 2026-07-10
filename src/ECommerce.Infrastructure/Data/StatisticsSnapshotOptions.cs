namespace ECommerce.Infrastructure.Data;

public sealed class StatisticsSnapshotOptions
{
    public const string SectionName = "StatisticsSnapshot";

    public bool Enabled { get; init; } = true;

    public int RefreshIntervalMinutes { get; init; } = 5;

    public int InitialBackfillDays { get; init; } = 30;
}
