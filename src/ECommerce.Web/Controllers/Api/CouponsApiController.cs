using ECommerce.Application.DTOs;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1")]
public sealed class CouponsApiController : ApiControllerBase
{
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
    public ActionResult<ApiResponse<PagedResult<CouponTemplateDto>>> SearchTemplates([FromQuery] CouponTemplateQuery query)
    {
        return NotReady<PagedResult<CouponTemplateDto>>("Admin coupon template search endpoint is defined and awaiting implementation.");
    }

    [HttpPost("admin/coupon-templates")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public ActionResult<ApiResponse<int>> CreateTemplate([FromBody] CouponTemplateRequest request)
    {
        return NotReady<int>("Admin coupon template create endpoint is defined and awaiting implementation.");
    }

    [HttpPut("admin/coupon-templates/{templateId:int}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public ActionResult<ApiResponse<object?>> UpdateTemplate(int templateId, [FromBody] CouponTemplateRequest request)
    {
        return NotReady<object?>("Admin coupon template update endpoint is defined and awaiting implementation.");
    }

    [HttpPut("admin/coupon-templates/{templateId:int}/status")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public ActionResult<ApiResponse<object?>> SetTemplateStatus(int templateId, [FromBody] StatusUpdateRequest request)
    {
        return NotReady<object?>("Admin coupon template status endpoint is defined and awaiting implementation.");
    }
}
