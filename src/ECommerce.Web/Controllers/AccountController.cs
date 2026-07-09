using System.Security.Claims;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("/account/login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost("/account/login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginPost(
        string username,
        string password,
        bool rememberMe,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _authService.LoginAsync(new LoginRequest(username, password, rememberMe), cancellationToken);
            await SignInAsync(session, rememberMe);
            return Redirect(GetLoginRedirectUrl(session));
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Login");
        }
    }

    [HttpGet("/account/register")]
    [AllowAnonymous]
    public IActionResult Register()
    {
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
        await HttpContext.SignOutAsync(AuthConstants.AuthenticationScheme);
        return Redirect("/account/login");
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
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
            });
    }

    private static string GetLoginRedirectUrl(UserSessionDto session)
    {
        return session.Roles.Any(role =>
            string.Equals(role, AuthConstants.Roles.Admin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, AuthConstants.Roles.Service, StringComparison.OrdinalIgnoreCase))
            ? "/admin"
            : "/";
    }
}
