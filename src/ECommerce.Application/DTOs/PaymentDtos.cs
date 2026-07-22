namespace ECommerce.Application.DTOs;

public sealed record SimulatePaymentRequest(
    long OrderId,
    string PayMethod);

public sealed record SimulatedPaymentCallback(
    long OrderId,
    string TradeNo,
    int Status,
    decimal PayAmount,
    string RawData,
    string Signature);

public sealed record PaymentDto(
    long PaymentId,
    long OrderId,
    string PayMethod,
    int Status,
    string? TradeNo,
    decimal PayAmount,
    DateTime? PaidAt);

public sealed record PaymentResultDto(
    bool Paid,
    string TradeNo,
    PaymentDto Payment);
