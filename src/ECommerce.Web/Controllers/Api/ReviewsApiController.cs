using ECommerce.Application.DTOs;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1")]
public sealed class ReviewsApiController : ApiControllerBase
{
    [HttpPost("reviews")]
    [Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
    public ActionResult<ApiResponse<long>> Create([FromBody] ReviewRequest request)
    {
        return NotReady<long>("Review create endpoint is defined and awaiting implementation.");
    }

    [HttpGet("products/{productId:long}/reviews")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<PagedResult<ReviewDto>>> ProductReviews(long productId, [FromQuery] PageQuery query)
    {
        return NotReady<PagedResult<ReviewDto>>("Product review list endpoint is defined and awaiting implementation.");
    }

    [HttpGet("admin/reviews")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public ActionResult<ApiResponse<PagedResult<ReviewDto>>> AdminReviews([FromQuery] ReviewQuery query)
    {
        return NotReady<PagedResult<ReviewDto>>("Admin review search endpoint is defined and awaiting implementation.");
    }

    [HttpPut("admin/reviews/{reviewId:long}/status")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public ActionResult<ApiResponse<object?>> SetStatus(long reviewId, [FromBody] StatusUpdateRequest request)
    {
        return NotReady<object?>("Review status endpoint is defined and awaiting implementation.");
    }
}
