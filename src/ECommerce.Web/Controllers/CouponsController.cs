using ECommerce.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

[Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
public sealed class CouponsController : Controller
{
    [HttpGet("/coupons")]
    public IActionResult Index()
    {
        return View();
    }
}
