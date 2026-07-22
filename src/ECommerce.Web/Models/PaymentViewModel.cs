using ECommerce.Application.DTOs;

namespace ECommerce.Web.Models;

public sealed record PaymentViewModel(
    OrderPaymentContextDto Order,
    PaymentDto Payment,
    string? Notice);
