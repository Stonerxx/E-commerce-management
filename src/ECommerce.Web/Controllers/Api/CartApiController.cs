using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/cart")]
[Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
public sealed class CartApiController : ApiControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartApiController> _logger;

    public CartApiController(ICartService cartService, ILogger<CartApiController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户的购物车
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetCart(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var cart = await _cartService.GetCartAsync(userId, cancellationToken);
        return Ok(ApiResponse<CartDto>.Ok(cart));
    }

    /// <summary>
    /// 添加商品到购物车
    /// </summary>
    [HttpPost("items")]
    public async Task<ActionResult<ApiResponse<object>>> AddItem(
        [FromBody] CartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        await _cartService.AddItemAsync(userId, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, message: "已添加到购物车"));
    }

    /// <summary>
    /// 修改购物车项（数量或选中状态）
    /// </summary>
    [HttpPut("items/{cartItemId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateItem(
        long cartItemId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        await _cartService.UpdateItemAsync(userId, cartItemId, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, message: "已更新"));
    }

    /// <summary>
    /// 删除购物车项
    /// </summary>
    [HttpDelete("items/{cartItemId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveItem(
        long cartItemId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        await _cartService.RemoveItemAsync(userId, cartItemId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, message: "已删除"));
    }

    /// <summary>
    /// 清空购物车
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult<ApiResponse<object>>> Clear(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        await _cartService.ClearAsync(userId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, message: "已清空购物车"));
    }
}
