using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Infrastructure.Repositories;

public sealed class OperationLogRepository : IOperationLogRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public OperationLogRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task WriteAsync(OperationLogRequest request, CancellationToken cancellationToken = default)
    {
        var log = new OperationLog
        {
            OperatorId = request.OperatorId,
            OperatorName = request.OperatorName,
            Module = request.Module,
            Action = request.Action,
            Description = request.Description,
            IpAddress = request.IpAddress,
            RequestParams = request.RequestParams,
            Result = request.Result
        };

        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        AttachTransaction(command);
        command.CommandText = """
            INSERT INTO OPERATION_LOG(operator_id, operator_name, module, action, description, ip_address, request_params, result)
            VALUES (:operator_id, :operator_name, :module, :action, :description, :ip_address, :request_params, :result)
            """;
        command.Parameters.Add(new OracleParameter("operator_id", log.OperatorId));
        command.Parameters.Add(new OracleParameter("operator_name", log.OperatorName));
        command.Parameters.Add(new OracleParameter("module", log.Module));
        command.Parameters.Add(new OracleParameter("action", log.Action));
        command.Parameters.Add(new OracleParameter("description", (object?)log.Description ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("ip_address", log.IpAddress));
        command.Parameters.Add(new OracleParameter("request_params", (object?)log.RequestParams ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("result", log.Result));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<PagedResult<OperationLogDto>> SearchAsync(OperationLogQuery query, CancellationToken cancellationToken = default)
    {
        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var offset = (pageIndex - 1) * pageSize;
        var module = string.IsNullOrWhiteSpace(query.Module) ? null : query.Module.Trim();

        var connection = await GetConnectionAsync(cancellationToken);

        await using var countCommand = connection.CreateCommand();
        countCommand.BindByName = true;
        AttachTransaction(countCommand);
        countCommand.CommandText = """
            SELECT COUNT(1)
            FROM OPERATION_LOG
            WHERE (:operator_id IS NULL OR operator_id = :operator_id)
              AND (:module IS NULL OR module = :module)
              AND (:start_time IS NULL OR created_at >= :start_time)
              AND (:end_time IS NULL OR created_at <= :end_time)
            """;
        AddQueryParameters(countCommand, query.OperatorId, module, query.StartTime, query.EndTime);
        var totalCount = Convert.ToInt64(await countCommand.ExecuteScalarAsync(cancellationToken));

        await using var dataCommand = connection.CreateCommand();
        dataCommand.BindByName = true;
        AttachTransaction(dataCommand);
        dataCommand.CommandText = """
            SELECT id, operator_id, operator_name, module, action, description, ip_address, result, created_at
            FROM OPERATION_LOG
            WHERE (:operator_id IS NULL OR operator_id = :operator_id)
              AND (:module IS NULL OR module = :module)
              AND (:start_time IS NULL OR created_at >= :start_time)
              AND (:end_time IS NULL OR created_at <= :end_time)
            ORDER BY id DESC
            OFFSET :offset ROWS FETCH NEXT :page_size ROWS ONLY
            """;
        AddQueryParameters(dataCommand, query.OperatorId, module, query.StartTime, query.EndTime);
        dataCommand.Parameters.Add(new OracleParameter("offset", offset));
        dataCommand.Parameters.Add(new OracleParameter("page_size", pageSize));

        var logs = new List<OperationLog>();
        await using var reader = await dataCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            logs.Add(MapLog(reader));
        }

        return new PagedResult<OperationLogDto>(logs.Select(ToDto).ToArray(), pageIndex, pageSize, totalCount);
    }

    private static void AddQueryParameters(
        OracleCommand command,
        long? operatorId,
        string? module,
        DateTime? startTime,
        DateTime? endTime)
    {
        command.Parameters.Add(new OracleParameter("operator_id", (object?)operatorId ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("module", (object?)module ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("start_time", (object?)startTime ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("end_time", (object?)endTime ?? DBNull.Value));
    }

    private static OperationLog MapLog(System.Data.IDataRecord reader)
    {
        return new OperationLog
        {
            Id = Convert.ToInt64(reader["id"]),
            OperatorId = Convert.ToInt64(reader["operator_id"]),
            OperatorName = Convert.ToString(reader["operator_name"]) ?? string.Empty,
            Module = Convert.ToString(reader["module"]) ?? string.Empty,
            Action = Convert.ToString(reader["action"]) ?? string.Empty,
            Description = reader["description"] == DBNull.Value ? null : Convert.ToString(reader["description"]),
            IpAddress = Convert.ToString(reader["ip_address"]) ?? string.Empty,
            Result = Convert.ToInt32(reader["result"]),
            CreatedAt = Convert.ToDateTime(reader["created_at"])
        };
    }

    private static OperationLogDto ToDto(OperationLog log)
    {
        return new OperationLogDto(
            log.Id,
            log.OperatorId,
            log.OperatorName,
            log.Module,
            log.Action,
            log.Description,
            log.IpAddress,
            log.Result,
            log.CreatedAt);
    }

    private async Task<OracleConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        return (OracleConnection)await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
    }

    private void AttachTransaction(OracleCommand command)
    {
        if (_unitOfWork.CurrentTransaction is not null)
        {
            command.Transaction = (OracleTransaction)_unitOfWork.CurrentTransaction;
        }
    }
}
