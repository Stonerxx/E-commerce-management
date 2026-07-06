using ECommerce.Application.DTOs;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/payments")]
[Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
public sealed class PaymentsApiController : ApiControllerBase
{
    [HttpPost("simulate")]
    public ActionResult<ApiResponse<PaymentResultDto>> Simulate([FromBody] SimulatePaymentRequest request)
    {
        return NotReady<PaymentResultDto>("Simulated payment endpoint is defined and awaiting implementation.");
    }

    [HttpGet("{orderId:long}")]
    public ActionResult<ApiResponse<PaymentDto>> GetByOrder(long orderId)
    {
        return NotReady<PaymentDto>("Payment query endpoint is defined and awaiting implementation.");
    }

    [HttpPost("callback/simulated")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<object?>> SimulatedCallback([FromBody] SimulatedPaymentCallback request)
    {
        return NotReady<object?>("Simulated payment callback endpoint is defined and awaiting implementation.");
    }
}
