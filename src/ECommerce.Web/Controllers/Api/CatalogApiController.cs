using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1")]
[AllowAnonymous]
public sealed class CatalogApiController : ApiControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;

    public CatalogApiController(ICategoryService categoryService, IProductService productService)
    {
        _categoryService = categoryService;
        _productService = productService;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryTreeDto>>>> Categories(CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetTreeAsync(false, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CategoryTreeDto>>.Ok(categories));
    }

    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductListItemDto>>>> Products([FromQuery] ProductQuery query, CancellationToken cancellationToken)
    {
        var effectiveQuery = query with { Status = 1 };
        var result = await _productService.SearchAsync(effectiveQuery, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProductListItemDto>>.Ok(result));
    }

    [HttpGet("products/{productId:long}")]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> ProductDetail(long productId, CancellationToken cancellationToken)
    {
        var product = await _productService.GetPublicDetailAndTrackAsync(productId, cancellationToken);
        return Ok(ApiResponse<ProductDetailDto>.Ok(product));
    }
}
