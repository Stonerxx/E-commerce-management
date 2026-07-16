using System.Data;
using System.Data.Common;
using ECommerce.Domain.Entities;
using ECommerce.Shared.Abstractions;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Infrastructure.Repositories;

public class LogisticsRepository : ILogisticsRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public LogisticsRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private DbConnection Connection => _unitOfWork.CurrentConnection ?? throw new InvalidOperationException("Connection not opened.");
    private DbTransaction? Transaction => _unitOfWork.CurrentTransaction;

    private static DbParameter CreateParameter(string name, object? value)
    {
        return new OracleParameter(name, value ?? DBNull.Value);
    }

    public async Task<long> InsertLogisticsAsync(Logistics logistics, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO logistics 
                (order_id, company_name, tracking_no, shipped_at, status)
            VALUES 
                (:OrderId, :CompanyName, :TrackingNo, :ShippedAt, :Status)
            RETURNING id INTO :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        cmd.Parameters.Add(CreateParameter("OrderId", logistics.OrderId));
        cmd.Parameters.Add(CreateParameter("CompanyName", logistics.CompanyName));
        cmd.Parameters.Add(CreateParameter("TrackingNo", logistics.TrackingNo));
        cmd.Parameters.Add(CreateParameter("ShippedAt", logistics.ShippedAt));
        cmd.Parameters.Add(CreateParameter("Status", logistics.Status));

        var pId = cmd.CreateParameter();
        pId.ParameterName = "Id";
        pId.DbType = DbType.Int64;
        pId.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(pId);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        logistics.Id = Convert.ToInt64(pId.Value);
        return logistics.Id;
    }

    public async Task<long> InsertTrackAsync(LogisticsTrack track, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO logistics_track 
                (logistics_id, track_desc, track_time, location)
            VALUES 
                (:LogisticsId, :TrackDesc, :TrackTime, :Location)
            RETURNING id INTO :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;

        cmd.Parameters.Add(CreateParameter("LogisticsId", track.LogisticsId));
        cmd.Parameters.Add(CreateParameter("TrackDesc", track.TrackDesc));
        cmd.Parameters.Add(CreateParameter("TrackTime", track.TrackTime));
        cmd.Parameters.Add(CreateParameter("Location", track.Location));

        var pId = cmd.CreateParameter();
        pId.ParameterName = "Id";
        pId.DbType = DbType.Int64;
        pId.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(pId);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        track.Id = Convert.ToInt64(pId.Value);
        return track.Id;
    }

    public async Task<Logistics?> GetLogisticsByOrderIdAsync(long orderId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, order_id, company_name, tracking_no, shipped_at, status
            FROM logistics 
            WHERE order_id = :OrderId";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("OrderId", orderId));

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
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
        return null;
    }

    public async Task<Logistics?> GetLogisticsByIdAsync(long logisticsId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, order_id, company_name, tracking_no, shipped_at, status
            FROM logistics 
            WHERE id = :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("Id", logisticsId));

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
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
        return null;
    }

    public async Task<IReadOnlyList<LogisticsTrack>> GetTracksByLogisticsIdAsync(long logisticsId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, logistics_id, track_desc, track_time, location
            FROM logistics_track 
            WHERE logistics_id = :LogisticsId
            ORDER BY track_time DESC";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("LogisticsId", logisticsId));

        var items = new List<LogisticsTrack>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new LogisticsTrack
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                LogisticsId = reader.GetInt64(reader.GetOrdinal("logistics_id")),
                TrackDesc = reader.GetString(reader.GetOrdinal("track_desc")),
                TrackTime = reader.GetDateTime(reader.GetOrdinal("track_time")),
                Location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString(reader.GetOrdinal("location"))
            });
        }
        return items;
    }

    public async Task<bool> UpdateLogisticsStatusAsync(long logisticsId, int status, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE logistics 
            SET status = :Status
            WHERE id = :Id";

        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = Transaction;
        cmd.Parameters.Add(CreateParameter("Status", status));
        cmd.Parameters.Add(CreateParameter("Id", logisticsId));

        return await cmd.ExecuteNonQueryAsync(cancellationToken) > 0;
    }
}
