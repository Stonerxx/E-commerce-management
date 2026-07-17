using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Exceptions;
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

    [HttpGet("logistics/{orderId:long}")]
    [Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
    public async Task<ActionResult<ApiResponse<LogisticsDto>>> GetByOrder(
        long orderId,
        CancellationToken cancellationToken)
    {
        var logistics = await _logisticsService.GetByOrderAsync(GetCurrentUserId(), orderId, cancellationToken)
            ?? throw new BusinessException("LOGISTICS_NOT_FOUND", "物流信息不存在");
        return Ok(ApiResponse<LogisticsDto>.Ok(logistics));
    }

    [HttpPost("admin/orders/{orderId:long}/shipments")]
    [Authorize(Policy = AuthConstants.Policies.ServiceOrAdmin)]
    public async Task<ActionResult<ApiResponse<object?>>> Ship(
        long orderId,
        [FromBody] ShipmentRequest request,
        CancellationToken cancellationToken)
    {
        await _logisticsService.ShipAsync(
            orderId,
            request,
            GetCurrentUserId(),
            GetCurrentUserName(),
            GetClientIpAddress(),
            cancellationToken);
        return Ok(ApiResponse<object?>.Ok(null));
    }

    [HttpPost("admin/logistics/{logisticsId:long}/tracks")]
    [Authorize(Policy = AuthConstants.Policies.ServiceOrAdmin)]
    public async Task<ActionResult<ApiResponse<object?>>> AddTrack(
        long logisticsId,
        [FromBody] LogisticsTrackRequest request,
        CancellationToken cancellationToken)
    {
        await _logisticsService.AddTrackAsync(
            logisticsId,
            request,
            GetCurrentUserId(),
            GetCurrentUserName(),
            GetClientIpAddress(),
            cancellationToken);
        return Ok(ApiResponse<object?>.Ok(null));
    }
}
