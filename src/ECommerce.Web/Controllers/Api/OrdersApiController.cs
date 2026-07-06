using ECommerce.Application.DTOs;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/orders")]
[Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
public sealed class OrdersApiController : ApiControllerBase
{
    [HttpPost("preview")]
    public ActionResult<ApiResponse<OrderPreviewDto>> Preview([FromBody] CreateOrderRequest request)
    {
        return NotReady<OrderPreviewDto>("Order preview endpoint is defined and awaiting implementation.");
    }

    [HttpPost]
    public ActionResult<ApiResponse<long>> Create([FromBody] CreateOrderRequest request)
    {
        return NotReady<long>("Order create endpoint is defined and awaiting implementation.");
    }

    [HttpGet]
    public ActionResult<ApiResponse<PagedResult<OrderListItemDto>>> SearchMine([FromQuery] OrderQuery query)
    {
        return NotReady<PagedResult<OrderListItemDto>>("My order search endpoint is defined and awaiting implementation.");
    }

    [HttpGet("{orderId:long}")]
    public ActionResult<ApiResponse<OrderDetailDto>> Detail(long orderId)
    {
        return NotReady<OrderDetailDto>("Order detail endpoint is defined and awaiting implementation.");
    }

    [HttpPost("{orderId:long}/cancel")]
    public ActionResult<ApiResponse<object?>> Cancel(long orderId, [FromBody] CancelOrderRequest request)
    {
        return NotReady<object?>("Order cancel endpoint is defined and awaiting implementation.");
    }

    [HttpPost("{orderId:long}/confirm")]
    public ActionResult<ApiResponse<object?>> Confirm(long orderId)
    {
        return NotReady<object?>("Order confirm endpoint is defined and awaiting implementation.");
    }

    [HttpGet("{orderId:long}/logs")]
    public ActionResult<ApiResponse<IReadOnlyList<OrderLogDto>>> Logs(long orderId)
    {
        return NotReady<IReadOnlyList<OrderLogDto>>("Order log endpoint is defined and awaiting implementation.");
    }
}
