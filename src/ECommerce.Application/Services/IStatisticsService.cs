using ECommerce.Application.DTOs;

namespace ECommerce.Application.Services;

public interface IStatisticsService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);

    Task<OrderStatisticsDto> GetOrderStatisticsAsync(StatisticsQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(StatisticsQuery query, CancellationToken cancellationToken = default);
}
