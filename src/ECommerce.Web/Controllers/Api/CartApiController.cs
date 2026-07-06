using ECommerce.Application.DTOs;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/cart")]
[Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
public sealed class CartApiController : ApiControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<CartDto>> GetCart()
    {
        return NotReady<CartDto>("Cart query endpoint is defined and awaiting implementation.");
    }

    [HttpPost("items")]
    public ActionResult<ApiResponse<object?>> AddItem([FromBody] CartItemRequest request)
    {
        return NotReady<object?>("Cart add endpoint is defined and awaiting implementation.");
    }

    [HttpPut("items/{cartItemId:long}")]
    public ActionResult<ApiResponse<object?>> UpdateItem(long cartItemId, [FromBody] UpdateCartItemRequest request)
    {
        return NotReady<object?>("Cart update endpoint is defined and awaiting implementation.");
    }

    [HttpDelete("items/{cartItemId:long}")]
    public ActionResult<ApiResponse<object?>> RemoveItem(long cartItemId)
    {
        return NotReady<object?>("Cart remove endpoint is defined and awaiting implementation.");
    }

    [HttpDelete]
    public ActionResult<ApiResponse<object?>> Clear()
    {
        return NotReady<object?>("Cart clear endpoint is defined and awaiting implementation.");
    }
}
