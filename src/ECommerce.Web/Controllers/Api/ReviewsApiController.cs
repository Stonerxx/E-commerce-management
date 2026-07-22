using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1")]
public sealed class ReviewsApiController : ApiControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsApiController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost("reviews")]
    [Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
    public async Task<ActionResult<ApiResponse<long>>> Create(
        [FromBody] ReviewRequest request,
        CancellationToken cancellationToken)
    {
        var reviewId = await _reviewService.CreateAsync(GetCurrentUserId(), request, cancellationToken);
        return Ok(ApiResponse<long>.Ok(reviewId));
    }

    [HttpGet("products/{productId:long}/reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<ReviewDto>>>> ProductReviews(
        long productId,
        [FromQuery] PageQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.SearchByProductAsync(productId, query, cancellationToken);
        return Ok(ApiResponse<PagedResult<ReviewDto>>.Ok(result));
    }

    [HttpGet("admin/reviews")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReviewDto>>>> AdminReviews(
        [FromQuery] ReviewQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.SearchAdminAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResult<ReviewDto>>.Ok(result));
    }

    [HttpPut("admin/reviews/{reviewId:long}/status")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<object?>>> SetStatus(
        long reviewId,
        [FromBody] StatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _reviewService.SetStatusAsync(reviewId, request.Status, GetCurrentUserId(), cancellationToken);
        return Ok(ApiResponse<object?>.Ok(null));
    }
}
