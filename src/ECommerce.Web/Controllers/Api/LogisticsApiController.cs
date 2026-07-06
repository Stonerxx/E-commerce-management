using ECommerce.Application.DTOs;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1")]
public sealed class LogisticsApiController : ApiControllerBase
{
    [HttpGet("logistics/{orderId:long}")]
    [Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
    public ActionResult<ApiResponse<LogisticsDto>> GetByOrder(long orderId)
    {
        return NotReady<LogisticsDto>("Logistics query endpoint is defined and awaiting implementation.");
    }

    [HttpPost("admin/orders/{orderId:long}/shipments")]
    [Authorize(Policy = AuthConstants.Policies.ServiceOrAdmin)]
    public ActionResult<ApiResponse<object?>> Ship(long orderId, [FromBody] ShipmentRequest request)
    {
        return NotReady<object?>("Shipment endpoint is defined and awaiting implementation.");
    }

    [HttpPost("admin/logistics/{logisticsId:long}/tracks")]
    [Authorize(Policy = AuthConstants.Policies.ServiceOrAdmin)]
    public ActionResult<ApiResponse<object?>> AddTrack(long logisticsId, [FromBody] LogisticsTrackRequest request)
    {
        return NotReady<object?>("Logistics track endpoint is defined and awaiting implementation.");
    }
}
