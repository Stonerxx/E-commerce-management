using ClosedXML.Excel;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Data;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Text;
using System.Data;

namespace ECommerce.Infrastructure.Services;

class ExportService : IExportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StatisticsService> _logger;
    public ExportService(IUnitOfWork unitOfWork, ILogger<StatisticsService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FileExportDto> ExportOrdersAsync(AdminOrderQuery query, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync();


        // 1. 构建 SQL，左连接 PAYMENT 表获取支付时间和支付方式
        var sqlBuilder = new StringBuilder();
        sqlBuilder.AppendLine("SELECT");
        sqlBuilder.AppendLine("    om.ORDER_NO AS OrderNo,");
        sqlBuilder.AppendLine("    om.USER_ID AS UserId,");
        sqlBuilder.AppendLine("    om.STATUS AS Status,");
        sqlBuilder.AppendLine("    om.TOTAL_AMOUNT AS TotalAmount,");
        sqlBuilder.AppendLine("    CASE");
        sqlBuilder.AppendLine("        WHEN om.STATUS IN (0, 4) THEN 0");
        sqlBuilder.AppendLine("        ELSE NVL(om.PAY_AMOUNT, 0)");
        sqlBuilder.AppendLine("    END AS PayAmount,");
        sqlBuilder.AppendLine("    om.CREATED_AT AS CreatedAt,");
        sqlBuilder.AppendLine("    p.PAID_AT AS PaymentTime,");
        sqlBuilder.AppendLine("    p.PAY_METHOD AS PayMethod");
        sqlBuilder.AppendLine("FROM ORDER_MAIN om");
        sqlBuilder.AppendLine("LEFT JOIN PAYMENT p ON p.ORDER_ID = om.ID");
        sqlBuilder.AppendLine("WHERE 1=1");

        var parameters = new List<OracleParameter>();

        // 筛选条件
        if (query.UserId.HasValue && query.UserId.Value > 0)
        {
            sqlBuilder.AppendLine("    AND om.USER_ID = :UserId");
            parameters.Add(new OracleParameter(":UserId", OracleDbType.Int64) { Value = query.UserId.Value });
        }

        // 订单号模糊搜索
        if (!string.IsNullOrEmpty(query.OrderNo))
        {
            sqlBuilder.AppendLine("    AND om.ORDER_NO LIKE :OrderNo");
            parameters.Add(new OracleParameter(":OrderNo", OracleDbType.Varchar2) { Value = $"%{query.OrderNo}%" });
        }

        if (query.Status.HasValue)
        {
            sqlBuilder.AppendLine("    AND om.STATUS = :Status");
            parameters.Add(new OracleParameter(":Status", OracleDbType.Int32) { Value = query.Status.Value });
        }

        if (query.StartTime.HasValue)
        {
            sqlBuilder.AppendLine("    AND om.CREATED_AT >= :StartTime");
            parameters.Add(new OracleParameter(":StartTime", OracleDbType.Date) { Value = query.StartTime.Value });
        }

        if (query.EndTime.HasValue)
        {
            var endDate = query.EndTime.Value.Date.AddDays(1);
            sqlBuilder.AppendLine("    AND om.CREATED_AT < :EndTime");
            parameters.Add(new OracleParameter(":EndTime", OracleDbType.Date) { Value = endDate });
        }

        sqlBuilder.AppendLine("ORDER BY om.CREATED_AT DESC");

        // 导出数量限制（默认 5000）
        int pageSize = query.PageSize > 0 ? query.PageSize : 5000;
        if (pageSize > 5000) pageSize = 5000;

        var finalSql = $@"
        SELECT OrderNo, UserId, Status, TotalAmount, PayAmount, CreatedAt, PaymentTime, PayMethod
        FROM (
            {sqlBuilder}
        )
        WHERE ROWNUM <= :PageSize";

        parameters.Add(new OracleParameter(":PageSize", OracleDbType.Int32) { Value = pageSize });

        await using var command = connection.CreateCommand();
        command.CommandText = finalSql;
        command.Parameters.AddRange(parameters.ToArray());

        if (_unitOfWork.CurrentTransaction != null)
            command.Transaction = _unitOfWork.CurrentTransaction;

        // 2. 执行查询，填充 DataTable
        var dataTable = new DataTable();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        dataTable.Load(reader);

        // 3. 使用 ClosedXML 生成 Excel
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Orders");

        if (dataTable.Rows.Count == 0)
        {
            worksheet.Cell(1, 1).Value = "暂无符合条件的订单数据";
        }
        else
        {
            // 直接插入数据（列名英文，稍后替换）
            worksheet.Cell(1, 1).InsertTable(dataTable);

            // 设置中文列头（必须与 SELECT 列顺序一致）
            worksheet.Cell(1, 1).Value = "订单编号";
            worksheet.Cell(1, 2).Value = "用户ID";
            worksheet.Cell(1, 3).Value = "状态";
            worksheet.Cell(1, 4).Value = "商品总额";
            worksheet.Cell(1, 5).Value = "实付金额";
            worksheet.Cell(1, 6).Value = "创建时间";
            worksheet.Cell(1, 7).Value = "支付时间";
            worksheet.Cell(1, 8).Value = "支付方式";

            // 状态列（第3列）将数字转为中文
            for (int row = 2; row <= dataTable.Rows.Count + 1; row++)
            {
                var statusCell = worksheet.Cell(row, 3);
                if (int.TryParse(statusCell.GetString(), out int statusVal))
                {
                    statusCell.Value = statusVal switch
                    {
                        0 => "待支付",
                        1 => "已支付",
                        2 => "已发货",
                        3 => "已完成",
                        4 => "已取消",
                        _ => "未知"
                    };
                }
            }

            // 设置金额列格式（保留两位小数）
            worksheet.Column(4).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Column(5).Style.NumberFormat.Format = "#,##0.00";

            // 自动调整列宽
            worksheet.Columns().AdjustToContents();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileBytes = stream.ToArray();

        return new FileExportDto(
            FileName: $"Orders_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            ContentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Content: fileBytes
        );
    }

    public async Task<FileExportDto> ExportInventoryAsync(InventoryLogQuery query, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync();

        // 1. 构建 SQL，关联 SKU 和 PRODUCT 获取商品名称和规格
        var sqlBuilder = new StringBuilder();
        sqlBuilder.AppendLine("SELECT");
        sqlBuilder.AppendLine("    l.ID AS LogId,");
        sqlBuilder.AppendLine("    p.NAME AS ProductName,");
        sqlBuilder.AppendLine("    s.SPEC_DESC AS SpecDesc,");
        sqlBuilder.AppendLine("    l.CHANGE_TYPE AS ChangeType,");
        sqlBuilder.AppendLine("    l.CHANGE_QTY AS ChangeQty,");
        sqlBuilder.AppendLine("    l.BEFORE_STOCK AS BeforeStock,");
        sqlBuilder.AppendLine("    l.AFTER_STOCK AS AfterStock,");
        sqlBuilder.AppendLine("    l.OPERATOR_ID AS OperatorId,");
        sqlBuilder.AppendLine("    l.REF_ORDER_ID AS RefOrderId,");
        sqlBuilder.AppendLine("    l.REMARK AS Remark,");
        sqlBuilder.AppendLine("    l.CREATED_AT AS CreatedAt");
        sqlBuilder.AppendLine("FROM INVENTORY_LOG l");
        sqlBuilder.AppendLine("LEFT JOIN SKU s ON s.ID = l.SKU_ID");
        sqlBuilder.AppendLine("LEFT JOIN PRODUCT p ON p.ID = s.PRODUCT_ID");
        sqlBuilder.AppendLine("WHERE 1=1");

        var parameters = new List<OracleParameter>();

        // SkuId 精确匹配
        if (query.SkuId.HasValue && query.SkuId.Value > 0)
        {
            sqlBuilder.AppendLine("    AND l.SKU_ID = :SkuId");
            parameters.Add(new OracleParameter(":SkuId", OracleDbType.Int64) { Value = query.SkuId.Value });
        }

        // ChangeType 精确匹配（字符串）
        if (!string.IsNullOrEmpty(query.ChangeType))
        {
            sqlBuilder.AppendLine("    AND l.CHANGE_TYPE = :ChangeType");
            parameters.Add(new OracleParameter(":ChangeType", OracleDbType.Varchar2) { Value = query.ChangeType });
        }

        // 时间范围
        if (query.StartTime.HasValue)
        {
            sqlBuilder.AppendLine("    AND l.CREATED_AT >= :StartTime");
            parameters.Add(new OracleParameter(":StartTime", OracleDbType.Date) { Value = query.StartTime.Value });
        }
        if (query.EndTime.HasValue)
        {
            var endDate = query.EndTime.Value.Date.AddDays(1);
            sqlBuilder.AppendLine("    AND l.CREATED_AT < :EndTime");
            parameters.Add(new OracleParameter(":EndTime", OracleDbType.Date) { Value = endDate });
        }

        sqlBuilder.AppendLine("ORDER BY l.CREATED_AT DESC");

        // 导出行数限制（忽略 PageIndex）
        int pageSize = query.PageSize > 0 ? query.PageSize : 5000;
        if (pageSize > 5000) pageSize = 5000;

        var finalSql = $@"
        SELECT LogId, ProductName, SpecDesc, ChangeType, ChangeQty, BeforeStock, AfterStock, OperatorId, RefOrderId, Remark, CreatedAt
        FROM (
            {sqlBuilder}
        )
        WHERE ROWNUM <= :PageSize";

        parameters.Add(new OracleParameter(":PageSize", OracleDbType.Int32) { Value = pageSize });

        await using var command = connection.CreateCommand();
        command.CommandText = finalSql;
        command.Parameters.AddRange(parameters.ToArray());

        if (_unitOfWork.CurrentTransaction != null)
            command.Transaction = _unitOfWork.CurrentTransaction;

        // 2. 执行查询，填充 DataTable
        var dataTable = new DataTable();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        dataTable.Load(reader);

        // 3. 使用 ClosedXML 生成 Excel
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("InventoryLogs");

        if (dataTable.Rows.Count == 0)
        {
            worksheet.Cell(1, 1).Value = "暂无符合条件的库存变动记录";
        }
        else
        {
            // 直接插入数据（列名为英文，稍后替换为中文）
            worksheet.Cell(1, 1).InsertTable(dataTable);

            // 设置中文表头（按列顺序）
            worksheet.Cell(1, 1).Value = "日志ID";
            worksheet.Cell(1, 2).Value = "商品名称";
            worksheet.Cell(1, 3).Value = "规格描述";
            worksheet.Cell(1, 4).Value = "变动类型";
            worksheet.Cell(1, 5).Value = "变动数量";
            worksheet.Cell(1, 6).Value = "变动前库存";
            worksheet.Cell(1, 7).Value = "变动后库存";
            worksheet.Cell(1, 8).Value = "操作人ID";
            worksheet.Cell(1, 9).Value = "关联订单ID";
            worksheet.Cell(1, 10).Value = "备注";
            worksheet.Cell(1, 11).Value = "创建时间";

            // 可选：将 ChangeType 英文转换为中文显示（但 InsertTable 已经填充了原始值，我们可以整列替换）
            // 如果需要，可以遍历行转换，但为了简单，我们不改，因为数据量大时性能好。如果业务要求，可以加。
            // 这里我们直接保持数据库中的英文值，但可以告诉用户。
            // 或者可以在 SQL 中用 CASE 转换，但为了保持通用，暂时不转。

            // 自动调整列宽
            worksheet.Columns().AdjustToContents();
        }

        // 4. 保存
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileBytes = stream.ToArray();

        return new FileExportDto(
            FileName: $"InventoryLogs_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            ContentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Content: fileBytes
        );
    }
}
