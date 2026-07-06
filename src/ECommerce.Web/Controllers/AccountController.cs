using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

public sealed class AccountController : Controller
{
    [HttpGet("/account/login")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost("/account/login")]
    [ValidateAntiForgeryToken]
    public IActionResult LoginPost()
    {
        ModelState.AddModelError(string.Empty, "Authentication service is not implemented yet.");
        return View("Login");
    }

    [HttpGet("/account/register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost("/account/register")]
    [ValidateAntiForgeryToken]
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
}
