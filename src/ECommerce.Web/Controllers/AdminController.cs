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

    [HttpGet("/admin/categories")]
    public IActionResult Categories()
    {
        return View();
    }

    [HttpGet("/admin/products")]
    public IActionResult Products()
    {
        return View();
    }

    [HttpGet("/admin/products/create")]
    public IActionResult CreateProduct()
    {
        return View();
    }

    [HttpGet("/admin/products/{id}/edit")]
    public IActionResult EditProduct(int id)
    {
        return View();
    }

    [HttpGet("/admin/skus")]
    public IActionResult Skus()
    {
        return View();
    }

    [HttpGet("/admin/inventory/warnings")]
    public IActionResult InventoryWarnings()
    {
        return View();
    }

    [HttpGet("/admin/inventory/logs")]
    public IActionResult InventoryLogs()
    {
        return View();
    }
}
