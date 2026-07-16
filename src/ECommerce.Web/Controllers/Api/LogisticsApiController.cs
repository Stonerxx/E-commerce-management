using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1")]
public sealed class LogisticsApiController : ApiControllerBase
{
    private readonly ILogisticsService _logisticsService;

    public LogisticsApiController(ILogisticsService logisticsService)
    {
        _logisticsService = logisticsService;
    }

    private long GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !long.TryParse(claim, out var userId))
            return 1;
        return userId;
    }

    [HttpGet("logistics/{orderId:long}")]
    [Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
    public async Task<ActionResult<ApiResponse<LogisticsDto>>> GetByOrder(long orderId)
    {
        var result = await _logisticsService.GetByOrderAsync(GetCurrentUserId(), orderId);
        if (result == null)
            return Ok(ApiResponse<LogisticsDto>.Fail(ErrorCodes.ResourceNotFound, "Logistics not found"));
            
        return Ok(ApiResponse<LogisticsDto>.Ok(result));
    }

    [HttpPost("admin/orders/{orderId:long}/shipments")]
    [Authorize(Policy = AuthConstants.Policies.ServiceOrAdmin)]
    public async Task<ActionResult<ApiResponse<object?>>> Ship(long orderId, [FromBody] ShipmentRequest request)
    {
        await _logisticsService.ShipAsync(orderId, request, GetCurrentUserId());
        return Ok(ApiResponse<object?>.Ok(null));
    }

    [HttpPost("admin/logistics/{logisticsId:long}/tracks")]
    [Authorize(Policy = AuthConstants.Policies.ServiceOrAdmin)]
    public async Task<ActionResult<ApiResponse<object?>>> AddTrack(long logisticsId, [FromBody] LogisticsTrackRequest request)
    {
        await _logisticsService.AddTrackAsync(logisticsId, request, GetCurrentUserId());
        return Ok(ApiResponse<object?>.Ok(null));
    }
}
