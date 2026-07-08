using ECommerce.Shared.Constants;
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

    private readonly IConfiguration _configuration;

    public AccountController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("/account/login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["DemoAuthEnabled"] = IsDemoAuthEnabled();
        ViewData["DemoAuthPassword"] = GetDemoAuthPassword();
        return View();
    }

    [HttpPost("/account/login")]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> LoginPost(
        [FromForm] string username,
        [FromForm] string password,
        [FromForm] string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["DemoAuthEnabled"] = IsDemoAuthEnabled();
        ViewData["DemoAuthPassword"] = GetDemoAuthPassword();

        if (!IsDemoAuthEnabled())
        {
            ModelState.AddModelError(string.Empty, "Demo 登录已关闭，等待 member2 接入真实认证服务。");
            return View("Login");
        }

        if (!TryValidateDemoLogin(username, password, out var account))
        {
            ModelState.AddModelError(string.Empty, "演示账号或密码错误。");
            return View("Login");
        }

        // TEMP_DEMO_AUTH: 临时演示登录。member2 真实 AuthService 合入后删除此分支逻辑。
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.UserId.ToString()),
            new(ClaimTypes.Name, account.Username),
            new(ClaimTypes.Role, account.Role)
        };

        var identity = new ClaimsIdentity(claims, AuthConstants.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(AuthConstants.AuthenticationScheme, principal, properties);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost("/account/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthConstants.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("/account/register")]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost("/account/register")]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public IActionResult RegisterPost()
    {
        ModelState.AddModelError(string.Empty, "Registration service is not implemented yet.");
        return View("Register");
    }

    [HttpGet("/account/access-denied")]
    public IActionResult AccessDenied()
    {
        return View();
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
        return !string.IsNullOrWhiteSpace(username)
            && DemoAccounts.TryGetValue(username.Trim(), out account)
            && password == GetDemoAuthPassword();
    }

    private readonly record struct DemoAccount(long UserId, string Username, string Role);
}
