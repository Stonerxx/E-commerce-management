using ECommerce.Application.DTOs;

namespace ECommerce.Web.Models;

public sealed record DemoPaymentViewModel(
    OrderPaymentContextDto Order,
    string? Notice);
