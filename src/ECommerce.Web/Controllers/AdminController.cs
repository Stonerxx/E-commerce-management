using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

public sealed class AdminController : Controller
{
    [HttpGet("/admin")]
    [HttpGet("/admin/dashboard")]
    public IActionResult Dashboard()
    {
        return View();
    }
}
