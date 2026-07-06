namespace ECommerce.Shared.Contracts;

public record PageQuery
{
    public int PageIndex { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
