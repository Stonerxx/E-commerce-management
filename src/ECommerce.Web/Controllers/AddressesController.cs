using ECommerce.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

[Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
public sealed class AddressesController : Controller
{
    [HttpGet("/addresses")]
    public IActionResult Index()
    {
        return View();
    }
}
