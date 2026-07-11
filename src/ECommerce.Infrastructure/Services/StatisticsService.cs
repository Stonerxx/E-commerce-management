using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Enums;
using ECommerce.Shared.Abstractions;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Text;
// using Dapper;

namespace ECommerce.Infrastructure.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StatisticsService> _logger;
    private readonly IStatisticsSnapshotService _snapshotService;

    // 静态字段：记录上次快照刷新时间（UTC）
    private static DateTime _lastSnapshotRefreshUtc = DateTime.MinValue;
    private static readonly object _refreshLock = new object();
    public StatisticsService(
        IUnitOfWork unitOfWork, 
        ILogger<StatisticsService> logger, 
        IStatisticsSnapshotService snapshotService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _snapshotService = snapshotService;
    }

    private async Task RefreshSnapshotIfNeededAsync(CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        // 如果距离上次刷新不足 10 分钟，直接返回
        if ((nowUtc - _lastSnapshotRefreshUtc).TotalMinutes < 10)
            return;

        // 加锁防止并发时重复刷新
        lock (_refreshLock)
        {
            // 双重检查
            if ((nowUtc - _lastSnapshotRefreshUtc).TotalMinutes < 10)
                return;

            // 先更新时间，防止其他线程再进入
            _lastSnapshotRefreshUtc = nowUtc;
        }

        try
        {
            _logger.LogInformation("开始刷新订单统计快照（最近30天）...");
            await _snapshotService.RefreshRecentDaysAsync(30, cancellationToken);
            _logger.LogInformation("订单统计快照刷新完成。");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新订单统计快照失败，将允许下次重试。");
            // 刷新失败时重置时间，允许后续请求重试
            lock (_refreshLock)
            {
                _lastSnapshotRefreshUtc = DateTime.MinValue;
            }
            // 不向上抛出异常，避免影响统计查询（可继续读旧快照）
        }
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        await RefreshSnapshotIfNeededAsync(cancellationToken);

        var connection = await _unitOfWork.GetOpenConnectionAsync();
        var today = DateTime.Today;

        // 1. 从快照表取今日订单数和销售额
        await using var cmd1 = connection.CreateCommand();
        cmd1.CommandText = @"
        SELECT 
            NVL(ORDER_COUNT, 0) AS OrderCount,
            NVL(SALES_AMOUNT, 0) AS SalesAmount
        FROM ORDER_STAT_SNAPSHOT
        WHERE STAT_DATE = :Today";
        cmd1.Parameters.Add(new OracleParameter(":Today", OracleDbType.Date) { Value = today });
        if (_unitOfWork.CurrentTransaction != null) cmd1.Transaction = _unitOfWork.CurrentTransaction;

        int todayOrderCount = 0;
        decimal todaySalesAmount = 0;
        await using var reader = await cmd1.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            todayOrderCount = reader.GetInt32(reader.GetOrdinal("OrderCount"));
            todaySalesAmount = reader.GetDecimal(reader.GetOrdinal("SalesAmount"));
        }

        // 2. 待发货订单数（ORDER_MAIN 表）
        await using var cmd2 = connection.CreateCommand();
        cmd2.CommandText = $"SELECT COUNT(*) FROM ORDER_MAIN WHERE STATUS = {(int)OrderStatus.Paid}";
        if (_unitOfWork.CurrentTransaction != null) cmd2.Transaction = _unitOfWork.CurrentTransaction;
        var pendingShipmentCount = Convert.ToInt32(await cmd2.ExecuteScalarAsync(cancellationToken) ?? 0);

        // 3. 库存预警数（SKU + PRODUCT）
        await using var cmd3 = connection.CreateCommand();
        cmd3.CommandText = $@"
        SELECT COUNT(*) 
        FROM SKU s
        INNER JOIN PRODUCT p ON p.ID = s.PRODUCT_ID
        WHERE (s.STOCK - s.LOCKED_STOCK) <= s.WARNING_STOCK 
          AND s.STATUS = {(int)SkuStatus.Enabled}
          AND p.STATUS = {(int)ProductStatus.OnShelf}";
        if (_unitOfWork.CurrentTransaction != null) cmd3.Transaction = _unitOfWork.CurrentTransaction;
        var inventoryWarningCount = Convert.ToInt32(await cmd3.ExecuteScalarAsync(cancellationToken) ?? 0);

        // 4. 待审核评价数（REVIEW 表）
        await using var cmd4 = connection.CreateCommand();
        cmd4.CommandText = $"SELECT COUNT(*) FROM REVIEW WHERE STATUS = {(int)ReviewStatus.Pending}";
        if (_unitOfWork.CurrentTransaction != null) cmd4.Transaction = _unitOfWork.CurrentTransaction;
        var pendingReviewCount = Convert.ToInt32(await cmd4.ExecuteScalarAsync(cancellationToken) ?? 0);

        return new DashboardSummaryDto(
            TodayOrderCount: todayOrderCount,
            PendingShipmentCount: pendingShipmentCount,
            TodaySalesAmount: todaySalesAmount,
            InventoryWarningCount: inventoryWarningCount,
            PendingReviewCount: pendingReviewCount);
    }

    public async Task<OrderStatisticsDto> GetOrderStatisticsAsync(StatisticsQuery query, CancellationToken cancellationToken = default)
    {
        await RefreshSnapshotIfNeededAsync(cancellationToken);

        var connection = await _unitOfWork.GetOpenConnectionAsync();

        // 1. 根据维度决定分组表达式
        string groupByExpr = query.Dimension?.ToLower() switch
        {
            "month" => "TRUNC(CREATED_AT, 'MM')",
            _ => "TRUNC(CREATED_AT)" // 默认按天
        };

        // 2. 构建 SQL（从 ORDER_MAIN 直接查询，保证灵活性）
        var sqlBuilder = new StringBuilder();
        sqlBuilder.AppendLine($@"
        SELECT 
            {groupByExpr} AS ""Date"",
            COUNT(*) AS OrderCount,
            COUNT(CASE WHEN STATUS = {(int)OrderStatus.Paid} THEN 1 END) AS PaidCount,
            NVL(SUM(TOTAL_AMOUNT), 0) AS SalesAmount
        FROM ORDER_MAIN
        WHERE 1=1
          AND CREATED_AT >= :StartDate
          AND CREATED_AT < :EndDate");

        // 3. 状态筛选（如果传了具体的 Status 值）
        if (query.status.HasValue)
        {
            sqlBuilder.AppendLine("    AND STATUS = :Status");
        }

        sqlBuilder.AppendLine($"GROUP BY {groupByExpr}");
        sqlBuilder.AppendLine($"ORDER BY {groupByExpr} ASC");

        var finalSql = sqlBuilder.ToString();

        await using var command = connection.CreateCommand();
        command.CommandText = finalSql;
        command.Parameters.Add(new OracleParameter(":StartDate", OracleDbType.Date) { Value = query.StartDate.Date });
        command.Parameters.Add(new OracleParameter(":EndDate", OracleDbType.Date) { Value = query.EndDate.Date.AddDays(1) });

        if (query.status.HasValue)
        {
            command.Parameters.Add(new OracleParameter(":Status", OracleDbType.Int32) { Value = (int)query.status.Value });
        }

        if (_unitOfWork.CurrentTransaction != null)
            command.Transaction = _unitOfWork.CurrentTransaction;

        // 4. 执行查询（后续代码和之前完全一样，保持不变）
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var dateOrd = reader.GetOrdinal("Date");
        var orderCountOrd = reader.GetOrdinal("OrderCount");
        var paidCountOrd = reader.GetOrdinal("PaidCount");
        var salesAmountOrd = reader.GetOrdinal("SalesAmount");

        var points = new List<OrderStatisticPointDto>();
        int totalOrder = 0;
        int totalPaid = 0;
        decimal totalSales = 0;

        while (await reader.ReadAsync(cancellationToken))
        {
            var point = new OrderStatisticPointDto(
                Date: reader.GetDateTime(dateOrd),
                OrderCount: reader.GetInt32(orderCountOrd),
                PaidCount: reader.GetInt32(paidCountOrd),
                SalesAmount: reader.GetDecimal(salesAmountOrd)
            );
            points.Add(point);
            totalOrder += point.OrderCount;
            totalPaid += point.PaidCount;
            totalSales += point.SalesAmount;
        }

        decimal avgOrderAmount = totalOrder > 0
            ? Math.Round(totalSales / totalOrder, 2, MidpointRounding.AwayFromZero)
            : 0;

        return new OrderStatisticsDto(
            Points: points,
            OrderCount: totalOrder,
            PaidCount: totalPaid,
            SalesAmount: totalSales,
            AvgOrderAmount: avgOrderAmount);
    }
    
    public async Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(StatisticsQuery query, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync();

        await using var command = connection.CreateCommand();
        
        command.CommandText = $@"
            SELECT 
                p.ID AS ""ProductId"",
                p.NAME AS ""ProductName"",
                p.MAIN_IMAGE AS ""MainImage"",
                SUM(oi.QUANTITY) AS ""SalesCount"",
                SUM(oi.QUANTITY * oi.UNIT_PRICE) AS ""SalesAmount""
            FROM ORDER_ITEM oi
            INNER JOIN SKU s ON s.ID = oi.SKU_ID
            INNER JOIN PRODUCT p ON p.ID = s.PRODUCT_ID
            INNER JOIN ORDER_MAIN om ON om.ID = oi.ORDER_ID
            WHERE om.STATUS = :StatusPaid
              AND om.CREATED_AT >= :StartDate 
              AND om.CREATED_AT < :EndDate
            GROUP BY p.ID, p.NAME, p.MAIN_IMAGE
            ORDER BY ""SalesAmount"" DESC
            FETCH FIRST 10 ROWS ONLY";

        command.Parameters.Add(new OracleParameter(":StatusPaid", OracleDbType.Int32) { Value = (int)OrderStatus.Paid });
        command.Parameters.Add(new OracleParameter(":StartDate", OracleDbType.Date) { Value = query.StartDate });
        command.Parameters.Add(new OracleParameter(":EndDate", OracleDbType.Date) { Value = query.EndDate });

        if (_unitOfWork.CurrentTransaction != null)
            command.Transaction = _unitOfWork.CurrentTransaction;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        // 缓存列索引
        var prodIdOrd = reader.GetOrdinal("ProductId");
        var prodNameOrd = reader.GetOrdinal("ProductName");
        var mainImgOrd = reader.GetOrdinal("MainImage");
        var salesCountOrd = reader.GetOrdinal("SalesCount");
        var salesAmountOrd = reader.GetOrdinal("SalesAmount");

        var result = new List<TopProductDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new TopProductDto(
                ProductId: reader.GetInt64(prodIdOrd),
                ProductName: reader.GetString(prodNameOrd),
                MainImage: reader.IsDBNull(mainImgOrd) ? string.Empty : reader.GetString(mainImgOrd),
                SalesCount: reader.GetInt32(salesCountOrd),
                SalesAmount: reader.GetDecimal(salesAmountOrd)
            ));
        }

        return result.AsReadOnly();
    }
}