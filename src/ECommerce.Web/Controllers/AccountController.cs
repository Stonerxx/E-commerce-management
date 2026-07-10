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
    private static readonly IReadOnlyDictionary<string, DemoAccount> DemoAccounts =
        new Dictionary<string, DemoAccount>(StringComparer.OrdinalIgnoreCase)
        {
            ["demo_admin"] = new(9001, "demo_admin", AuthConstants.Roles.Admin),
            ["demo_service"] = new(9002, "demo_service", AuthConstants.Roles.Service),
            ["demo_user"] = new(9003, "demo_user", AuthConstants.Roles.User),
            ["demo_buyer"] = new(9004, "demo_buyer", AuthConstants.Roles.User)
        };

    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AccountController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    [HttpGet("/account/login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
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
            // TEMP_DEMO_AUTH: seed_demo_data.sql 仍使用占位 password_hash 时，保留演示账号登录能力。
            if (TryValidateDemoLogin(username, password, out var demoAccount))
            {
                var demoSession = new UserSessionDto(demoAccount.UserId, demoAccount.Username, new[] { demoAccount.Role });
                await SignInAsync(demoSession, rememberMe);
                return RedirectAfterLogin(demoSession, returnUrl);
            }

            var session = await _authService.LoginAsync(new LoginRequest(username, password, rememberMe), cancellationToken);
            await SignInAsync(session, rememberMe);
            return RedirectAfterLogin(session, returnUrl);
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

    private void SetLoginViewData(string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["DemoAuthEnabled"] = IsDemoAuthEnabled();
        ViewData["DemoAuthPassword"] = GetDemoAuthPassword();
    }

    private bool IsDemoAuthEnabled()
    {
        return _configuration.GetValue("DemoAuth:Enabled", true);
    }

    private string GetDemoAuthPassword()
    {
        return _configuration["DemoAuth:Password"] ?? "demo123";
    }

    private bool TryValidateDemoLogin(string username, string password, out DemoAccount account)
    {
        account = default;
        return IsDemoAuthEnabled()
            && !string.IsNullOrWhiteSpace(username)
            && DemoAccounts.TryGetValue(username.Trim(), out account)
            && password == GetDemoAuthPassword();
    }

    private readonly record struct DemoAccount(long UserId, string Username, string Role);
}
