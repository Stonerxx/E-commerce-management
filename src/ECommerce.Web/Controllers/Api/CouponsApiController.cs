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

    public CouponsApiController(ICouponService couponService)
    {
        _couponService = couponService;
    }

    private long GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !long.TryParse(claim, out var userId))
            return 1;
        return userId;
    }

    [HttpGet("coupons")]
    [Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
    public ActionResult<ApiResponse<IReadOnlyList<UserCouponDto>>> Mine()
    {
        return NotReady<IReadOnlyList<UserCouponDto>>("My coupon endpoint is defined and awaiting implementation.");
    }

    [HttpGet("coupon-templates/available")]
    [Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
    public ActionResult<ApiResponse<IReadOnlyList<CouponTemplateDto>>> Available()
    {
        return NotReady<IReadOnlyList<CouponTemplateDto>>("Available coupon template endpoint is defined and awaiting implementation.");
    }

    [HttpPost("coupon-templates/{templateId:int}/receive")]
    [Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
    public ActionResult<ApiResponse<object?>> Receive(int templateId)
    {
        return NotReady<object?>("Coupon receive endpoint is defined and awaiting implementation.");
    }

    [HttpPost("coupons/{userCouponId:long}/validate")]
    [Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
    public ActionResult<ApiResponse<CouponValidationDto>> Validate(long userCouponId, [FromBody] CouponValidationRequest request)
    {
        return NotReady<CouponValidationDto>("Coupon validation endpoint is defined and awaiting implementation.");
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
