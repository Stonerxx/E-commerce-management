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

    [HttpGet("/admin/statistics")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public IActionResult Statistics()
    {
        return View("Dashboard");
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

    [HttpGet("/admin/categories")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public IActionResult Categories()
    {
        return View();
    }

    [HttpGet("/admin/products")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public IActionResult Products()
    {
        return View();
    }

    [HttpGet("/admin/products/create")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public IActionResult CreateProduct()
    {
        return View();
    }

    [HttpGet("/admin/products/{id}/edit")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public IActionResult EditProduct(int id)
    {
        return View();
    }

    [HttpGet("/admin/skus")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public IActionResult Skus()
    {
        return View();
    }

    [HttpGet("/admin/inventory/warnings")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public IActionResult InventoryWarnings()
    {
        return View();
    }

    [HttpGet("/admin/inventory/logs")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public IActionResult InventoryLogs()
    {
        return View();
    }
}
