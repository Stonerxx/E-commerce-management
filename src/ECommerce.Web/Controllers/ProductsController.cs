using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

[AllowAnonymous]
public sealed class ProductsController : Controller
{
    [HttpGet("/products")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("/products/{productId:long}")]
    public IActionResult Detail(long productId)
    {
        ViewData["ProductId"] = productId;
        return View();
    }
}
