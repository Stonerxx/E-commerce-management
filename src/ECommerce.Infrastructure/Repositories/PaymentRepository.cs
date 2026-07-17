using System.Data;
using System.Data.Common;
using ECommerce.Domain.Entities;
using ECommerce.Shared.Abstractions;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Infrastructure.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public PaymentRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Payment?> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT id, order_id, pay_method, status, trade_no, pay_amount, paid_at
            FROM payment
            WHERE order_id = :OrderId
            """;

        await using var command = CreateCommand(connection, sql);
        command.Parameters.Add(CreateParameter("OrderId", orderId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<long> InsertAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO payment (order_id, pay_method, status, trade_no, pay_amount, paid_at, callback_data)
            VALUES (:OrderId, :PayMethod, :Status, :TradeNo, :PayAmount, :PaidAt, :CallbackData)
            RETURNING id INTO :Id
            """;

        await using var command = CreateCommand(connection, sql);
        command.Parameters.Add(CreateParameter("OrderId", payment.OrderId));
        command.Parameters.Add(CreateParameter("PayMethod", payment.PayMethod));
        command.Parameters.Add(CreateParameter("Status", payment.Status));
        command.Parameters.Add(CreateParameter("TradeNo", payment.TradeNo));
        command.Parameters.Add(CreateParameter("PayAmount", payment.PayAmount));
        command.Parameters.Add(CreateParameter("PaidAt", payment.PaidAt));
        command.Parameters.Add(CreateClobParameter("CallbackData", payment.CallbackData));

        var idParameter = command.CreateParameter();
        idParameter.ParameterName = "Id";
        idParameter.DbType = DbType.Int64;
        idParameter.Direction = ParameterDirection.Output;
        command.Parameters.Add(idParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
        payment.Id = Convert.ToInt64(idParameter.Value);
        return payment.Id;
    }

    public async Task<bool> TryMarkSuccessAsync(
        long paymentId,
        decimal expectedAmount,
        string tradeNo,
        DateTime paidAt,
        string callbackData,
        CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE payment
            SET status = 1,
                trade_no = :TradeNo,
                paid_at = :PaidAt,
                callback_data = :CallbackData
            WHERE id = :PaymentId
              AND status IN (0, 2)
              AND pay_amount = :ExpectedAmount
            """;

        await using var command = CreateCommand(connection, sql);
        command.Parameters.Add(CreateParameter("TradeNo", tradeNo));
        command.Parameters.Add(CreateParameter("PaidAt", paidAt));
        command.Parameters.Add(CreateClobParameter("CallbackData", callbackData));
        command.Parameters.Add(CreateParameter("PaymentId", paymentId));
        command.Parameters.Add(CreateParameter("ExpectedAmount", expectedAmount));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> TryMarkFailedAsync(
        long paymentId,
        string callbackData,
        CancellationToken cancellationToken = default)
    {
        var connection = await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE payment
            SET status = 2,
                trade_no = NULL,
                paid_at = NULL,
                callback_data = :CallbackData
            WHERE id = :PaymentId
              AND status = 0
            """;

        await using var command = CreateCommand(connection, sql);
        command.Parameters.Add(CreateClobParameter("CallbackData", callbackData));
        command.Parameters.Add(CreateParameter("PaymentId", paymentId));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private DbCommand CreateCommand(DbConnection connection, string sql)
    {
        var command = connection.CreateCommand();
        if (command is OracleCommand oracleCommand)
        {
            oracleCommand.BindByName = true;
        }

        command.CommandText = sql;
        command.Transaction = _unitOfWork.CurrentTransaction;
        return command;
    }

    private static Payment Map(DbDataReader reader)
    {
        return new Payment
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            OrderId = reader.GetInt64(reader.GetOrdinal("order_id")),
            PayMethod = reader.GetString(reader.GetOrdinal("pay_method")),
            Status = reader.GetInt32(reader.GetOrdinal("status")),
            TradeNo = reader.IsDBNull(reader.GetOrdinal("trade_no")) ? null : reader.GetString(reader.GetOrdinal("trade_no")),
            PayAmount = reader.GetDecimal(reader.GetOrdinal("pay_amount")),
            PaidAt = reader.IsDBNull(reader.GetOrdinal("paid_at")) ? null : reader.GetDateTime(reader.GetOrdinal("paid_at"))
        };
    }

    private static DbParameter CreateParameter(string name, object? value)
    {
        return new OracleParameter(name, value ?? DBNull.Value);
    }

    private static DbParameter CreateClobParameter(string name, string? value)
    {
        return new OracleParameter(name, OracleDbType.Clob)
        {
            Value = value ?? (object)DBNull.Value
        };
    }
}
