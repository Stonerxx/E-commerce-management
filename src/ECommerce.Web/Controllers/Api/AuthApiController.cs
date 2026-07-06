using ECommerce.Application.DTOs;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/auth")]
public sealed class AuthApiController : ApiControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<UserSessionDto>> Register([FromBody] RegisterRequest request)
    {
        return NotReady<UserSessionDto>("Register endpoint is defined and awaiting implementation.");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<UserSessionDto>> Login([FromBody] LoginRequest request)
    {
        return NotReady<UserSessionDto>("Login endpoint is defined and awaiting implementation.");
    }

    [HttpPost("logout")]
    public ActionResult<ApiResponse<object?>> Logout()
    {
        return NotReady<object?>("Logout endpoint is defined and awaiting implementation.");
    }

    [HttpGet("me")]
    public ActionResult<ApiResponse<UserSessionDto>> Me()
    {
        return NotReady<UserSessionDto>("Current user endpoint is defined and awaiting implementation.");
    }
}
