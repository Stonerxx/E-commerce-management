namespace ECommerce.Infrastructure.Data;

public sealed class PaymentOptions
{
    public const string SectionName = "Payment";

    public string SimulatedCallbackSecret { get; init; } = string.Empty;

    public bool HasUsableCallbackSecret =>
        !string.IsNullOrWhiteSpace(SimulatedCallbackSecret)
        && !SimulatedCallbackSecret.Contains("change_me", StringComparison.OrdinalIgnoreCase);
}
