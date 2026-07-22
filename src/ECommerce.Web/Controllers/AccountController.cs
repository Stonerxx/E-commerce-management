using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAuthService authService, ILogger<AccountController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet("/account/login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectAfterAuthentication();
        }

        SetLoginViewData(returnUrl);
        return View();
    }

    [HttpPost("/account/login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginPost(
        string username,
        string password,
        bool rememberMe,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        SetLoginViewData(returnUrl);

        try
        {
            var session = await _authService.LoginAsync(new LoginRequest(username, password, rememberMe), cancellationToken);
            await SignInAsync(session, rememberMe);
            return RedirectAfterLogin(session, returnUrl);
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed unexpectedly. TraceId: {TraceId}", HttpContext.TraceIdentifier);
            ModelState.AddModelError(
                string.Empty,
                $"登录失败：请检查服务器数据库连接和演示数据是否已更新。TraceId: {HttpContext.TraceIdentifier}");
            return View("Login");
        }
    }

    [HttpGet("/account/register")]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectAfterAuthentication();
        }

        return View();
    }

    [HttpPost("/account/register")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterPost(
        string username,
        string password,
        string? phone,
        string? email,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _authService.RegisterAsync(new RegisterRequest(username, password, phone, email), cancellationToken);
            await SignInAsync(session, rememberMe: false);
            return Redirect("/");
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Register");
        }
    }

    [HttpPost("/account/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        await HttpContext.SignOutAsync(AuthConstants.AuthenticationScheme);
        return Redirect("/");
    }

    [HttpGet("/account/access-denied")]
    public IActionResult AccessDenied()
    {
        return View();
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
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
            });
    }

    private IActionResult RedirectAfterLogin(UserSessionDto session, string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return Redirect(GetLoginRedirectUrl(session));
    }

    private static string GetLoginRedirectUrl(UserSessionDto session)
    {
        return session.Roles.Any(role =>
            string.Equals(role, AuthConstants.Roles.Admin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, AuthConstants.Roles.Service, StringComparison.OrdinalIgnoreCase))
            ? "/admin"
            : "/";
    }

    private IActionResult RedirectAfterAuthentication()
    {
        return Redirect(User.IsInRole(AuthConstants.Roles.Admin) || User.IsInRole(AuthConstants.Roles.Service)
            ? "/admin"
            : "/");
    }

    private void SetLoginViewData(string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
    }
}
