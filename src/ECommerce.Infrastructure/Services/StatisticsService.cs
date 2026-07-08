using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Enums;
using ECommerce.Shared.Abstractions;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
// using Dapper;

namespace ECommerce.Infrastructure.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StatisticsService> _logger;
    public StatisticsService(IUnitOfWork unitOfWork, ILogger<StatisticsService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync();

        // ---- 1. 今日订单数（待支付 + 已支付都算今日下单，用创建时间筛选） ----
        var today = DateTime.Today;
        await using var cmd1 = connection.CreateCommand();
        cmd1.CommandText = @"
        SELECT COUNT(*) 
        FROM ORDER_MAIN 
        WHERE CREATED_AT >= :Today AND CREATED_AT < :Tomorrow";
        cmd1.Parameters.Add(new OracleParameter(":Today", OracleDbType.Date) { Value = today });
        cmd1.Parameters.Add(new OracleParameter(":Tomorrow", OracleDbType.Date) { Value = today.AddDays(1) });
        if (_unitOfWork.CurrentTransaction != null) cmd1.Transaction = _unitOfWork.CurrentTransaction;
        var todayOrderCount = Convert.ToInt32(await cmd1.ExecuteScalarAsync(cancellationToken) ?? 0);

        // ---- 2. 待发货订单数（已支付 Paid = 1） ----
        await using var cmd2 = connection.CreateCommand();
        cmd2.CommandText = $"SELECT COUNT(*) FROM ORDER_MAIN WHERE STATUS = {(int)OrderStatus.Paid}";
        if (_unitOfWork.CurrentTransaction != null) cmd2.Transaction = _unitOfWork.CurrentTransaction;
        var pendingShipmentCount = Convert.ToInt32(await cmd2.ExecuteScalarAsync(cancellationToken) ?? 0);

        // ---- 3. 今日销售额（已支付的订单，用创建时间筛选） ----
        await using var cmd3 = connection.CreateCommand();
        cmd3.CommandText = @"
        SELECT NVL(SUM(TOTAL_AMOUNT), 0) 
        FROM ORDER_MAIN 
        WHERE STATUS = :StatusPaid 
          AND CREATED_AT >= :Today 
          AND CREATED_AT < :Tomorrow";
        cmd3.Parameters.Add(new OracleParameter(":StatusPaid", OracleDbType.Int32) { Value = (int)OrderStatus.Paid });
        cmd3.Parameters.Add(new OracleParameter(":Today", OracleDbType.Date) { Value = today });
        cmd3.Parameters.Add(new OracleParameter(":Tomorrow", OracleDbType.Date) { Value = today.AddDays(1) });
        if (_unitOfWork.CurrentTransaction != null) cmd3.Transaction = _unitOfWork.CurrentTransaction;
        var todaySalesAmount = Convert.ToDecimal(await cmd3.ExecuteScalarAsync(cancellationToken) ?? 0);

        // ---- 4. 库存预警数（SKU 在售 Enabled = 1，且可用库存 <= 警戒库存） ----
        await using var cmd4 = connection.CreateCommand();
        cmd4.CommandText = $@"
        SELECT COUNT(*) 
        FROM SKU s
        INNER JOIN PRODUCT p ON p.ID = s.PRODUCT_ID
        WHERE (s.STOCK - s.LOCKED_STOCK) <= s.WARNING_STOCK 
          AND s.STATUS = {(int)SkuStatus.Enabled}
          AND p.STATUS = {(int)ProductStatus.OnShelf}";
        if (_unitOfWork.CurrentTransaction != null) cmd4.Transaction = _unitOfWork.CurrentTransaction;
        var inventoryWarningCount = Convert.ToInt32(await cmd4.ExecuteScalarAsync(cancellationToken) ?? 0);

        // ---- 5. 待审核评价数（Pending = 0） ----
        await using var cmd5 = connection.CreateCommand();
        cmd5.CommandText = $"SELECT COUNT(*) FROM REVIEW WHERE STATUS = {(int)ReviewStatus.Pending}";
        if (_unitOfWork.CurrentTransaction != null) cmd5.Transaction = _unitOfWork.CurrentTransaction;
        var pendingReviewCount = Convert.ToInt32(await cmd5.ExecuteScalarAsync(cancellationToken) ?? 0);

        return new DashboardSummaryDto(
            TodayOrderCount: todayOrderCount,
            PendingShipmentCount: pendingShipmentCount,
            TodaySalesAmount: todaySalesAmount,
            InventoryWarningCount: inventoryWarningCount,
            PendingReviewCount: pendingReviewCount);
    }

    public async Task<OrderStatisticsDto> GetOrderStatisticsAsync(StatisticsQuery query, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT 
            s.STAT_DATE AS ""Date"", 
            s.ORDER_COUNT AS OrderCount, 
            s.PAID_COUNT AS PaidCount, 
            s.SALES_AMOUNT AS SalesAmount
        FROM ORDER_STAT_SNAPSHOT s
        WHERE s.STAT_DATE >= :StartDate AND s.STAT_DATE <= :EndDate
        ORDER BY s.STAT_DATE ASC";

        command.Parameters.Add(new OracleParameter(":StartDate", OracleDbType.Date) { Value = query.StartDate });
        command.Parameters.Add(new OracleParameter(":EndDate", OracleDbType.Date) { Value = query.EndDate });


        if (_unitOfWork.CurrentTransaction != null)
        {
            command.Transaction = _unitOfWork.CurrentTransaction;
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var points = new List<OrderStatisticPointDto>();
        
        int total_order = 0;
        int total_paid_order = 0;
        decimal total_sales_amount = 0;
        decimal avg_order_amount = 0;

        var dateOrdinal = reader.GetOrdinal("Date");
        var orderCountOrdinal = reader.GetOrdinal("OrderCount");
        var paidCountOrdinal = reader.GetOrdinal("PaidCount");
        var salesAmountOrdinal = reader.GetOrdinal("SalesAmount");

        while (await reader.ReadAsync(cancellationToken))
        {
            var point = new OrderStatisticPointDto(
                Date: reader.GetDateTime(dateOrdinal),
                OrderCount: reader.GetInt32(orderCountOrdinal),
                PaidCount: reader.GetInt32(paidCountOrdinal),
                SalesAmount: reader.GetDecimal(salesAmountOrdinal)
            );
        
            points.Add(point);
            total_order += point.OrderCount;
            total_paid_order += point.PaidCount;
            total_sales_amount += point.SalesAmount;
        }

        avg_order_amount = total_order > 0
            ? total_sales_amount / total_order
            : 0;

        var result = new OrderStatisticsDto(
            Points: points,
            OrderCount: total_order,
            PaidCount: total_paid_order,
            SalesAmount: total_sales_amount,
            AvgOrderAmount: avg_order_amount);

        return result;
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