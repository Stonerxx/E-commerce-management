using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1")]
public sealed class CouponsApiController : ApiControllerBase
{
    private readonly ICouponService _couponService;
    private readonly IOrderService _orderService;

    public CouponsApiController(ICouponService couponService, IOrderService orderService)
    {
        _couponService = couponService;
        _orderService = orderService;
    }

    [HttpGet("coupons")]
    [Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserCouponDto>>>> Mine(
        CancellationToken cancellationToken)
    {
        var result = await _couponService.GetMineAsync(GetCurrentUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UserCouponDto>>.Ok(result));
    }

    [HttpGet("coupon-templates/available")]
    [Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CouponTemplateDto>>>> Available(
        CancellationToken cancellationToken)
    {
        var result = await _couponService.GetAvailableTemplatesAsync(GetCurrentUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CouponTemplateDto>>.Ok(result));
    }

    [HttpPost("coupon-templates/{templateId:int}/receive")]
    [Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
    public async Task<ActionResult<ApiResponse<object?>>> Receive(
        int templateId,
        CancellationToken cancellationToken)
    {
        await _couponService.ReceiveAsync(GetCurrentUserId(), templateId, cancellationToken);
        return Ok(ApiResponse<object?>.Ok(null));
    }

    [HttpPost("coupons/{userCouponId:long}/validate")]
    [Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
    public async Task<ActionResult<ApiResponse<CouponValidationDto>>> Validate(
        long userCouponId,
        [FromBody] CouponValidationRequest request,
        CancellationToken cancellationToken)
    {
        var result = request.CartItemIds is { Count: > 0 }
            ? await _orderService.ValidateCouponAsync(
                GetCurrentUserId(),
                userCouponId,
                request.CartItemIds,
                cancellationToken)
            : await _couponService.ValidateAsync(
                GetCurrentUserId(),
                userCouponId,
                request.OrderAmount,
                cancellationToken);
        return Ok(ApiResponse<CouponValidationDto>.Ok(result));
    }

    [HttpGet("admin/coupon-templates")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<PagedResult<CouponTemplateDto>>>> SearchTemplates([FromQuery] CouponTemplateQuery query)
    {
        var result = await _couponService.SearchTemplatesAsync(query);
        return Ok(ApiResponse<PagedResult<CouponTemplateDto>>.Ok(result));
    }

    [HttpPost("admin/coupon-templates")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<int>>> CreateTemplate([FromBody] CouponTemplateRequest request)
    {
        var id = await _couponService.CreateTemplateAsync(request, GetCurrentUserId());
        return Ok(ApiResponse<int>.Ok(id));
    }

    [HttpPut("admin/coupon-templates/{templateId:int}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<object?>>> UpdateTemplate(int templateId, [FromBody] CouponTemplateRequest request)
    {
        await _couponService.UpdateTemplateAsync(templateId, request, GetCurrentUserId());
        return Ok(ApiResponse<object?>.Ok(null));
    }

    [HttpPut("admin/coupon-templates/{templateId:int}/status")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<object?>>> SetTemplateStatus(int templateId, [FromBody] StatusUpdateRequest request)
    {
        await _couponService.SetTemplateStatusAsync(templateId, request.Status, GetCurrentUserId());
        return Ok(ApiResponse<object?>.Ok(null));
    }
}
