using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

/// <summary>
/// 购物车页面
/// </summary>
[Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
public sealed class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService cartService, ILogger<CartController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    /// <summary>
    /// 购物车页面
    /// </summary>
    [HttpGet("/cart")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        // 采用 Vue + API 方式
        return View();
    }
}
