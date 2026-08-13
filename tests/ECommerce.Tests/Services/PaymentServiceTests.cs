using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Exceptions;
using Microsoft.Extensions.Options;
using Moq;

namespace ECommerce.Tests.Services;

public sealed class PaymentServiceTests
{
    private const string CallbackSecret = "unit-test-callback-secret-123456";

    private readonly Mock<IPaymentRepository> _payments = new();
    private readonly Mock<IOrderService> _orders = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _unitOfWork.Setup(item => item.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(item => item.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(item => item.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _service = new PaymentService(
            _payments.Object,
            _orders.Object,
            _unitOfWork.Object,
            Options.Create(new PaymentOptions { SimulatedCallbackSecret = CallbackSecret }));
    }

    [Fact]
    public async Task CreateOrGetPendingAsync_CreatesOnePaymentForPendingOrder()
    {
        _orders.Setup(item => item.GetPaymentContextAsync(7, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder());
        _payments.SetupSequence(item => item.GetByOrderIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null)
            .ReturnsAsync((Payment?)null);
        _payments.Setup(item => item.InsertAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(20);

        var result = await _service.CreateOrGetPendingAsync(7, 10);

        Assert.Equal(20, result.PaymentId);
        Assert.Equal((int)PaymentStatus.Pending, result.Status);
        Assert.Equal(88m, result.PayAmount);
        _unitOfWork.Verify(item => item.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SimulatePayAsync_UpdatesPaymentAndOrderInOneTransaction()
    {
        var payment = CreatePayment();
        _orders.Setup(item => item.GetPaymentContextAsync(7, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder());
        _payments.SetupSequence(item => item.GetByOrderIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment)
            .ReturnsAsync(payment);
        _payments.Setup(item => item.TryMarkSuccessAsync(
                20,
                88m,
                It.Is<string>(value => value.StartsWith("SIM", StringComparison.Ordinal)),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.SimulatePayAsync(7, new SimulatePaymentRequest(10, "模拟支付"));

        Assert.True(result.Paid);
        Assert.Equal((int)PaymentStatus.Success, result.Payment.Status);
        _orders.Verify(item => item.MarkPaidAsync(10, 20, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(item => item.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SimulatePayAsync_ReturnsExistingSuccessIdempotently()
    {
        var payment = CreatePayment(PaymentStatus.Success);
        payment.TradeNo = "SIM-EXISTING";
        payment.PaidAt = DateTime.Now;
        _orders.Setup(item => item.GetPaymentContextAsync(7, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(OrderStatus.Paid));
        _payments.Setup(item => item.GetByOrderIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var result = await _service.SimulatePayAsync(7, new SimulatePaymentRequest(10, "模拟支付"));

        Assert.Equal("SIM-EXISTING", result.TradeNo);
        _orders.Verify(item => item.MarkPaidAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(item => item.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncSimulatedCallbackAsync_RejectsInvalidSignature()
    {
        var request = new SimulatedPaymentCallback(10, "TRADE-1", 1, 88m, "{}", "invalid");

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _service.SyncSimulatedCallbackAsync(request));

        Assert.Equal("PAYMENT_SIGNATURE_INVALID", exception.Code);
        _payments.Verify(item => item.GetByOrderIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncSimulatedCallbackAsync_VerifiesSignatureAndMarksOrderPaid()
    {
        const string rawData = "{\"status\":\"success\"}";
        var request = new SimulatedPaymentCallback(
            10,
            "TRADE-1",
            (int)PaymentStatus.Success,
            88m,
            rawData,
            Sign(10, "TRADE-1", (int)PaymentStatus.Success, 88m, rawData));
        _payments.Setup(item => item.GetByOrderIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePayment());
        _payments.Setup(item => item.TryMarkSuccessAsync(
                20,
                88m,
                "TRADE-1",
                It.IsAny<DateTime>(),
                rawData,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.SyncSimulatedCallbackAsync(request);

        _orders.Verify(item => item.MarkPaidAsync(10, 20, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(item => item.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static OrderPaymentContextDto CreateOrder(OrderStatus status = OrderStatus.PendingPayment)
    {
        return new OrderPaymentContextDto(10, "ORDER-10", 7, (int)status, 88m, null, DateTime.Now.AddMinutes(20));
    }

    private static Payment CreatePayment(PaymentStatus status = PaymentStatus.Pending)
    {
        return new Payment
        {
            Id = 20,
            OrderId = 10,
            PayMethod = "模拟支付",
            Status = (int)status,
            PayAmount = 88m
        };
    }

    private static string Sign(long orderId, string tradeNo, int status, decimal amount, string rawData)
    {
        var payload = string.Join('|',
            orderId.ToString(CultureInfo.InvariantCulture),
            tradeNo,
            status.ToString(CultureInfo.InvariantCulture),
            amount.ToString("0.00", CultureInfo.InvariantCulture),
            rawData);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(CallbackSecret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }
}
