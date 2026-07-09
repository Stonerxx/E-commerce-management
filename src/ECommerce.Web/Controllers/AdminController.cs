using ECommerce.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

[Authorize(Policy = AuthConstants.Policies.ServiceOrAdmin)]
public sealed class AdminController : Controller
{
    [HttpGet("/admin")]
    [HttpGet("/admin/dashboard")]
    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet("/admin/users")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public IActionResult Users()
    {
        return View();
    }

    [HttpGet("/admin/permissions")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public IActionResult Permissions()
    {
        return View();
    }

    [HttpGet("/admin/operation-logs")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public IActionResult OperationLogs()
    {
        return View();
    }
}
