namespace ECommerce.Application.DTOs;

public sealed record StatisticsQuery(
    DateTime StartDate,
    DateTime EndDate,
    string Dimension);

public sealed record DashboardSummaryDto(
    int TodayOrderCount,
    int PendingShipmentCount,
    decimal TodaySalesAmount,
    int InventoryWarningCount,
    int PendingReviewCount);

public sealed record OrderStatisticsDto(
    IReadOnlyList<OrderStatisticPointDto> Points,
    int OrderCount,
    int PaidCount,
    decimal SalesAmount,
    decimal AvgOrderAmount);

public sealed record OrderStatisticPointDto(
    DateTime Date,
    int OrderCount,
    int PaidCount,
    decimal SalesAmount);

public sealed record TopProductDto(
    long ProductId,
    string ProductName,
    string MainImage,
    int SalesCount,
    decimal SalesAmount);
