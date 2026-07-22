using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/payments")]
[Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
public sealed class PaymentsApiController : ApiControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsApiController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("simulate")]
    public async Task<ActionResult<ApiResponse<PaymentResultDto>>> Simulate(
        [FromBody] SimulatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.SimulatePayAsync(GetCurrentUserId(), request, cancellationToken);
        return Ok(ApiResponse<PaymentResultDto>.Ok(result));
    }

    [HttpGet("{orderId:long}")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> GetByOrder(
        long orderId,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetByOrderAsync(GetCurrentUserId(), orderId, cancellationToken);
        return Ok(ApiResponse<PaymentDto>.Ok(result));
    }

    [HttpPost("callback/simulated")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object?>>> SimulatedCallback(
        [FromBody] SimulatedPaymentCallback request,
        CancellationToken cancellationToken)
    {
        await _paymentService.SyncSimulatedCallbackAsync(request, cancellationToken);
        return Ok(ApiResponse<object?>.Ok(null));
    }
}
