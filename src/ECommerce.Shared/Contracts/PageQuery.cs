namespace ECommerce.Shared.Contracts;

public record PageQuery
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    public int PageIndex { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;

    public int SafePageIndex => Math.Max(1, PageIndex);

    public int SafePageSize => Math.Clamp(PageSize, 1, MaximumPageSize);
}
