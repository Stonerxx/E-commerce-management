namespace ECommerce.Shared.Contracts;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageIndex,
    int PageSize,
    long TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static PagedResult<T> Empty(int pageIndex = 1, int pageSize = 20)
    {
        return new PagedResult<T>(Array.Empty<T>(), pageIndex, pageSize, 0);
    }
}
