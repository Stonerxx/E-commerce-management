using System.Security.Claims;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using ECommerce.Web.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/auth")]
[Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
public sealed class AuthApiController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthApiController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<UserSessionDto>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _authService.RegisterAsync(request, cancellationToken);
        await SignInAsync(session, rememberMe: false);
        return ApiResponse<UserSessionDto>.Ok(session, HttpContext.TraceIdentifier, "注册成功");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<UserSessionDto>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _authService.LoginAsync(request, cancellationToken);
        await SignInAsync(session, request.RememberMe);
        return ApiResponse<UserSessionDto>.Ok(session, HttpContext.TraceIdentifier, "登录成功");
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object?>>> Logout(CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(cancellationToken);
        await HttpContext.SignOutAsync(AuthConstants.AuthenticationScheme);
        return ApiResponse<object?>.Ok(null, HttpContext.TraceIdentifier, "退出登录成功");
    }

    [HttpGet("me")]
    public ActionResult<ApiResponse<UserSessionDto>> Me()
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        var session = new UserSessionDto(User.GetUserId(), User.GetUsername(), roles);
        return ApiResponse<UserSessionDto>.Ok(session, HttpContext.TraceIdentifier);
    }

    private async Task SignInAsync(UserSessionDto session, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new(ClaimTypes.Name, session.Username)
        };
        claims.AddRange(session.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, AuthConstants.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(
            AuthConstants.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
            });
    }
}
