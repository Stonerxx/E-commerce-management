using System.Data;
using System.Data.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using ECommerce.Shared.Abstractions;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Infrastructure.Repositories;

public sealed class LogisticsRepository : ILogisticsRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public LogisticsRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private DbConnection Connection => _unitOfWork.CurrentConnection
        ?? throw new InvalidOperationException("Connection not opened. Call GetOpenConnectionAsync first.");

    private DbTransaction? Transaction => _unitOfWork.CurrentTransaction;

    public async Task<Logistics?> GetByIdAsync(long logisticsId, CancellationToken cancellationToken = default)
    {
        return await GetAsync("id = :Id", "Id", logisticsId, cancellationToken);
    }

    public async Task<Logistics?> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken = default)
    {
        return await GetAsync("order_id = :OrderId", "OrderId", orderId, cancellationToken);
    }

    public async Task<long> InsertAsync(Logistics logistics, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO logistics (order_id, company_name, tracking_no, shipped_at, status)
            VALUES (:OrderId, :CompanyName, :TrackingNo, :ShippedAt, :Status)
            RETURNING id INTO :Id";

        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("OrderId", logistics.OrderId));
        command.Parameters.Add(CreateParameter("CompanyName", logistics.CompanyName));
        command.Parameters.Add(CreateParameter("TrackingNo", logistics.TrackingNo));
        command.Parameters.Add(CreateParameter("ShippedAt", logistics.ShippedAt));
        command.Parameters.Add(CreateParameter("Status", logistics.Status));

        var idParameter = command.CreateParameter();
        idParameter.ParameterName = "Id";
        idParameter.DbType = DbType.Int64;
        idParameter.Direction = ParameterDirection.Output;
        command.Parameters.Add(idParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
        logistics.Id = OracleValueConverter.ToInt64(idParameter.Value);
        return logistics.Id;
    }

    public async Task<long> InsertTrackAsync(LogisticsTrack track, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO logistics_track (logistics_id, track_desc, track_time, location)
            VALUES (:LogisticsId, :TrackDesc, :TrackTime, :Location)
            RETURNING id INTO :Id";

        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("LogisticsId", track.LogisticsId));
        command.Parameters.Add(CreateParameter("TrackDesc", track.TrackDesc));
        command.Parameters.Add(CreateParameter("TrackTime", track.TrackTime));
        command.Parameters.Add(CreateParameter("Location", track.Location));

        var idParameter = command.CreateParameter();
        idParameter.ParameterName = "Id";
        idParameter.DbType = DbType.Int64;
        idParameter.Direction = ParameterDirection.Output;
        command.Parameters.Add(idParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
        track.Id = OracleValueConverter.ToInt64(idParameter.Value);
        return track.Id;
    }

    public async Task<bool> TryUpdateStatusAsync(
        long logisticsId,
        int expectedStatus,
        int targetStatus,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE logistics
            SET status = :TargetStatus
            WHERE id = :LogisticsId
              AND status = :ExpectedStatus";

        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("TargetStatus", targetStatus));
        command.Parameters.Add(CreateParameter("LogisticsId", logisticsId));
        command.Parameters.Add(CreateParameter("ExpectedStatus", expectedStatus));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private async Task<Logistics?> GetAsync(
        string predicate,
        string parameterName,
        long parameterValue,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        var sql = $"SELECT * FROM logistics WHERE {predicate}";
        Logistics? logistics;
        await using (var command = CreateCommand(sql))
        {
            command.Parameters.Add(CreateParameter(parameterName, parameterValue));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            logistics = await reader.ReadAsync(cancellationToken) ? MapLogistics(reader) : null;
        }

        if (logistics is null)
        {
            return null;
        }

        logistics.Tracks = await GetTracksAsync(logistics.Id, cancellationToken);
        return logistics;
    }

    private async Task<IReadOnlyList<LogisticsTrack>> GetTracksAsync(
        long logisticsId,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT *
            FROM logistics_track
            WHERE logistics_id = :LogisticsId
            ORDER BY track_time, id";
        await using var command = CreateCommand(sql);
        command.Parameters.Add(CreateParameter("LogisticsId", logisticsId));

        var tracks = new List<LogisticsTrack>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tracks.Add(MapTrack(reader));
        }

        return tracks;
    }

    private DbCommand CreateCommand(string sql)
    {
        var command = Connection.CreateCommand();
        if (command is OracleCommand oracleCommand)
        {
            oracleCommand.BindByName = true;
        }

        command.CommandText = sql;
        command.Transaction = Transaction;
        return command;
    }

    private static Logistics MapLogistics(DbDataReader reader)
    {
        return new Logistics
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            OrderId = reader.GetInt64(reader.GetOrdinal("order_id")),
            CompanyName = reader.GetString(reader.GetOrdinal("company_name")),
            TrackingNo = reader.GetString(reader.GetOrdinal("tracking_no")),
            ShippedAt = reader.IsDBNull(reader.GetOrdinal("shipped_at")) ? null : reader.GetDateTime(reader.GetOrdinal("shipped_at")),
            Status = reader.GetInt32(reader.GetOrdinal("status"))
        };
    }

    private static LogisticsTrack MapTrack(DbDataReader reader)
    {
        return new LogisticsTrack
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            LogisticsId = reader.GetInt64(reader.GetOrdinal("logistics_id")),
            TrackDesc = reader.GetString(reader.GetOrdinal("track_desc")),
            TrackTime = reader.GetDateTime(reader.GetOrdinal("track_time")),
            Location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString(reader.GetOrdinal("location"))
        };
    }

    private static DbParameter CreateParameter(string name, object? value)
    {
        return new OracleParameter(name, value ?? DBNull.Value);
    }
}
