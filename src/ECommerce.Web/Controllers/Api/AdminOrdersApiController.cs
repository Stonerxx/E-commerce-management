using ECommerce.Application.DTOs;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/admin/orders")]
[Authorize(Policy = AuthConstants.Policies.ServiceOrAdmin)]
public sealed class AdminOrdersApiController : ApiControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<PagedResult<OrderListItemDto>>> Search([FromQuery] AdminOrderQuery query)
    {
        return NotReady<PagedResult<OrderListItemDto>>("Admin order search endpoint is defined and awaiting implementation.");
    }

    [HttpGet("{orderId:long}")]
    public ActionResult<ApiResponse<OrderDetailDto>> Detail(long orderId)
    {
        return NotReady<OrderDetailDto>("Admin order detail endpoint is defined and awaiting implementation.");
    }
}
