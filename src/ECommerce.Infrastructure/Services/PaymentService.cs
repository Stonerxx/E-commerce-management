using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Exceptions;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Infrastructure.Services;

public sealed class PaymentService : IPaymentService
{
    private const string SimulatedPayMethod = "模拟支付";

    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderService _orderService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PaymentOptions _options;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IOrderService orderService,
        IUnitOfWork unitOfWork,
        IOptions<PaymentOptions> options)
    {
        _paymentRepository = paymentRepository;
        _orderService = orderService;
        _unitOfWork = unitOfWork;
        _options = options.Value;
    }

    public async Task<PaymentDto> CreateOrGetPendingAsync(
        long userId,
        long orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifiers(userId, orderId);
        var order = await _orderService.GetPaymentContextAsync(userId, orderId, cancellationToken);
        var existing = await _paymentRepository.GetByOrderIdAsync(orderId, cancellationToken);
        if (existing is not null)
        {
            return Map(existing);
        }

        EnsureOrderCanStartPayment(order);
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            existing = await _paymentRepository.GetByOrderIdAsync(orderId, cancellationToken);
            if (existing is not null)
            {
                await _unitOfWork.CommitAsync(cancellationToken);
                return Map(existing);
            }

            var payment = CreatePendingPayment(order);
            payment.Id = await _paymentRepository.InsertAsync(payment, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            return Map(payment);
        }
        catch (OracleException exception) when (exception.Number == 1)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            existing = await _paymentRepository.GetByOrderIdAsync(orderId, cancellationToken);
            return existing is not null
                ? Map(existing)
                : throw new BusinessException("PAYMENT_CREATE_CONFLICT", "支付记录已变化，请刷新后重试");
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PaymentResultDto> SimulatePayAsync(
        long userId,
        SimulatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifiers(userId, request.OrderId);
        if (!string.Equals(request.PayMethod?.Trim(), SimulatedPayMethod, StringComparison.Ordinal))
        {
            throw new BusinessException("PAYMENT_METHOD_INVALID", "当前仅支持模拟支付");
        }

        var order = await _orderService.GetPaymentContextAsync(userId, request.OrderId, cancellationToken);
        var existing = await _paymentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (existing?.Status == (int)PaymentStatus.Success)
        {
            return CreatePaidResult(existing);
        }

        EnsureOrderCanStartPayment(order);
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var payment = await _paymentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
            if (payment?.Status == (int)PaymentStatus.Success)
            {
                await _unitOfWork.CommitAsync(cancellationToken);
                return CreatePaidResult(payment);
            }

            if (payment is null)
            {
                payment = CreatePendingPayment(order);
                payment.Id = await _paymentRepository.InsertAsync(payment, cancellationToken);
            }

            if (payment.Status == (int)PaymentStatus.Refunded)
            {
                throw new BusinessException("PAYMENT_STATUS_INVALID", "已退款支付不能重新支付");
            }

            if (payment.PayAmount != order.PayAmount)
            {
                throw new BusinessException("PAYMENT_AMOUNT_CHANGED", "支付金额与订单金额不一致");
            }

            var paidAt = DateTime.Now;
            var tradeNo = GenerateTradeNo(request.OrderId, paidAt);
            var callbackData = JsonSerializer.Serialize(new
            {
                channel = "simulated-page",
                status = "success",
                paidAt
            });
            if (!await _paymentRepository.TryMarkSuccessAsync(
                    payment.Id,
                    order.PayAmount,
                    tradeNo,
                    paidAt,
                    callbackData,
                    cancellationToken))
            {
                throw new BusinessException(ErrorCodes.PaymentAlreadyPaid, "支付状态已变化，请刷新后重试");
            }

            await _orderService.MarkPaidAsync(request.OrderId, payment.Id, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            payment.Status = (int)PaymentStatus.Success;
            payment.TradeNo = tradeNo;
            payment.PaidAt = paidAt;
            payment.CallbackData = callbackData;
            return CreatePaidResult(payment);
        }
        catch (OracleException exception) when (exception.Number == 1)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            var concurrent = await _paymentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
            if (concurrent?.Status == (int)PaymentStatus.Success)
            {
                return CreatePaidResult(concurrent);
            }

            throw new BusinessException("PAYMENT_CREATE_CONFLICT", "支付记录已由其他请求创建，请刷新后重试");
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task SyncSimulatedCallbackAsync(
        SimulatedPaymentCallback request,
        CancellationToken cancellationToken = default)
    {
        ValidateCallback(request);
        var payment = await _paymentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken)
            ?? throw new BusinessException("PAYMENT_NOT_FOUND", "支付记录不存在");
        if (payment.PayAmount != request.PayAmount)
        {
            throw new BusinessException("PAYMENT_AMOUNT_INVALID", "回调金额与支付记录不一致");
        }

        if (request.Status == (int)PaymentStatus.Success
            && payment.Status == (int)PaymentStatus.Success)
        {
            if (!string.Equals(payment.TradeNo, request.TradeNo, StringComparison.Ordinal))
            {
                throw new BusinessException("PAYMENT_TRADE_NO_CONFLICT", "支付流水号不一致");
            }

            return;
        }

        if (payment.Status == (int)PaymentStatus.Refunded)
        {
            throw new BusinessException("PAYMENT_STATUS_INVALID", "已退款支付不能更新回调状态");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (request.Status == (int)PaymentStatus.Success)
            {
                var paidAt = DateTime.Now;
                if (!await _paymentRepository.TryMarkSuccessAsync(
                        payment.Id,
                        request.PayAmount,
                        request.TradeNo.Trim(),
                        paidAt,
                        request.RawData,
                        cancellationToken))
                {
                    throw new BusinessException(ErrorCodes.PaymentAlreadyPaid, "支付状态已变化，请刷新后重试");
                }

                await _orderService.MarkPaidAsync(request.OrderId, payment.Id, cancellationToken);
            }
            else if (!await _paymentRepository.TryMarkFailedAsync(payment.Id, request.RawData, cancellationToken))
            {
                throw new BusinessException("PAYMENT_STATUS_CHANGED", "支付状态已变化，请刷新后重试");
            }

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PaymentDto> GetByOrderAsync(
        long userId,
        long orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifiers(userId, orderId);
        await _orderService.GetPaymentContextAsync(userId, orderId, cancellationToken);
        var payment = await _paymentRepository.GetByOrderIdAsync(orderId, cancellationToken)
            ?? throw new BusinessException("PAYMENT_NOT_FOUND", "支付记录不存在");
        return Map(payment);
    }

    private void ValidateCallback(SimulatedPaymentCallback request)
    {
        if (!_options.HasUsableCallbackSecret)
        {
            throw new BusinessException(ErrorCodes.ConfigurationError, "未配置模拟支付回调密钥");
        }

        if (request.OrderId <= 0
            || request.PayAmount < 0
            || request.Status is not ((int)PaymentStatus.Success) and not ((int)PaymentStatus.Failed)
            || string.IsNullOrWhiteSpace(request.TradeNo)
            || request.TradeNo.Trim().Length > 100
            || string.IsNullOrWhiteSpace(request.RawData)
            || request.RawData.Length > 10000
            || string.IsNullOrWhiteSpace(request.Signature))
        {
            throw new BusinessException("VALIDATION_ERROR", "支付回调参数无效");
        }

        var payload = string.Join('|',
            request.OrderId.ToString(CultureInfo.InvariantCulture),
            request.TradeNo.Trim(),
            request.Status.ToString(CultureInfo.InvariantCulture),
            request.PayAmount.ToString("0.00", CultureInfo.InvariantCulture),
            request.RawData);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SimulatedCallbackSecret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        var actual = request.Signature.Trim().ToUpperInvariant();
        if (actual.Length != expected.Length
            || !CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(actual),
                Encoding.ASCII.GetBytes(expected)))
        {
            throw new BusinessException("PAYMENT_SIGNATURE_INVALID", "支付回调签名无效");
        }
    }

    private static void ValidateIdentifiers(long userId, long orderId)
    {
        if (userId <= 0 || orderId <= 0)
        {
            throw new BusinessException("VALIDATION_ERROR", "用户 ID 和订单 ID 必须有效");
        }
    }

    private static void EnsureOrderCanStartPayment(OrderPaymentContextDto order)
    {
        if (order.Status != (int)OrderStatus.PendingPayment)
        {
            throw new BusinessException("ORDER_STATUS_INVALID", "当前订单状态不允许发起支付");
        }

        if (order.PayExpireTime <= DateTime.Now)
        {
            throw new BusinessException("PAYMENT_EXPIRED", "订单支付时间已过期");
        }
    }

    private static Payment CreatePendingPayment(OrderPaymentContextDto order)
    {
        return new Payment
        {
            OrderId = order.OrderId,
            PayMethod = SimulatedPayMethod,
            Status = (int)PaymentStatus.Pending,
            PayAmount = order.PayAmount
        };
    }

    private static string GenerateTradeNo(long orderId, DateTime paidAt)
    {
        return $"SIM{paidAt:yyyyMMddHHmmssfff}{orderId}{RandomNumberGenerator.GetInt32(100000, 999999)}";
    }

    private static PaymentResultDto CreatePaidResult(Payment payment)
    {
        if (string.IsNullOrWhiteSpace(payment.TradeNo))
        {
            throw new BusinessException("PAYMENT_DATA_INVALID", "成功支付缺少交易流水号");
        }

        return new PaymentResultDto(true, payment.TradeNo, Map(payment));
    }

    private static PaymentDto Map(Payment payment)
    {
        return new PaymentDto(
            payment.Id,
            payment.OrderId,
            payment.PayMethod,
            payment.Status,
            payment.TradeNo,
            payment.PayAmount,
            payment.PaidAt);
    }
}
