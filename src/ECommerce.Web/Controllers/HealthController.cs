using ECommerce.Infrastructure.Data;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

[ApiController]
public sealed class HealthController : ControllerBase
{
    private readonly IDatabaseHealthCheck _databaseHealthCheck;

    public HealthController(IDatabaseHealthCheck databaseHealthCheck)
    {
        _databaseHealthCheck = databaseHealthCheck;
    }

    [HttpGet("/health")]
    [HttpGet("/health/live")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<object>> Live()
    {
        var payload = new
        {
            status = "ok",
            application = "ECommerce.Web",
            time = DateTimeOffset.UtcNow
        };

        return Ok(ApiResponse<object>.Ok(payload, HttpContext.TraceIdentifier));
    }

    [HttpGet("/health/ready")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<DatabaseCheckResult>>> Ready(CancellationToken cancellationToken)
    {
        var result = await _databaseHealthCheck.CheckAsync(cancellationToken);
        var response = ApiResponse<DatabaseCheckResult>.Ok(result, HttpContext.TraceIdentifier);
        return result.Configured && result.Connected
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }

    [HttpGet("/api/v1/system/version")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<object>> Version()
    {
        var assembly = typeof(HealthController).Assembly.GetName();
        var payload = new
        {
            name = assembly.Name,
            version = assembly.Version?.ToString() ?? "0.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        };

        return Ok(ApiResponse<object>.Ok(payload, HttpContext.TraceIdentifier));
    }

    [HttpGet("/api/v1/system/db-check")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<DatabaseCheckResult>>> DatabaseCheck(CancellationToken cancellationToken)
    {
        var result = await _databaseHealthCheck.CheckAsync(cancellationToken);
        return Ok(ApiResponse<DatabaseCheckResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}
