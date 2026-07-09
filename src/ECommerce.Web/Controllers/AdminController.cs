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
}
