using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/admin")]
[Authorize(Policy = AuthConstants.Policies.AdminOnly)]
public sealed class AdminCatalogApiController : ApiControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;
    private readonly IProductImageService _productImageService;
    private readonly ISkuService _skuService;
    private readonly IInventoryService _inventoryService;

    public AdminCatalogApiController(
        ICategoryService categoryService,
        IProductService productService,
        IProductImageService productImageService,
        ISkuService skuService,
        IInventoryService inventoryService)
    {
        _categoryService = categoryService;
        _productService = productService;
        _productImageService = productImageService;
        _skuService = skuService;
        _inventoryService = inventoryService;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryTreeDto>>>> Categories(CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetTreeAsync(true, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CategoryTreeDto>>.Ok(categories));
    }

    [HttpPost("categories")]
    public async Task<ActionResult<ApiResponse<int>>> CreateCategory([FromBody] CategoryRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var categoryId = await _categoryService.CreateAsync(request, userId, cancellationToken);
        return Ok(ApiResponse<int>.Ok(categoryId));
    }

    [HttpPut("categories/{categoryId:int}")]
    public async Task<ActionResult<ApiResponse<object?>>> UpdateCategory(int categoryId, [FromBody] CategoryRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _categoryService.UpdateAsync(categoryId, request, userId, cancellationToken);
        return Ok(ApiResponse<object?>.Ok(null));
    }

    [HttpDelete("categories/{categoryId:int}")]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteCategory(int categoryId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _categoryService.DeleteOrDisableAsync(categoryId, userId, cancellationToken);
        return Ok(ApiResponse<object?>.Ok(null));
    }

    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductListItemDto>>>> Products([FromQuery] ProductQuery query, CancellationToken cancellationToken)
    {
        var result = await _productService.SearchAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProductListItemDto>>.Ok(result));
    }

    [HttpGet("products/{productId:long}")]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> GetProduct(long productId, CancellationToken cancellationToken)
    {
        var product = await _productService.GetDetailAsync(productId, cancellationToken);
        if (product == null)
        {
            return NotFound(ApiResponse<ProductDetailDto>.Fail("NOT_FOUND", "商品不存在"));
        }
        return Ok(ApiResponse<ProductDetailDto>.Ok(product));
    }

    [HttpPost("products")]
    public async Task<ActionResult<ApiResponse<long>>> CreateProduct([FromBody] ProductSaveRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var productId = await _productService.CreateAsync(request, userId, cancellationToken);
        return Ok(ApiResponse<long>.Ok(productId));
    }

    [HttpPut("products/{productId:long}")]
    public async Task<ActionResult<ApiResponse<object?>>> UpdateProduct(long productId, [FromBody] ProductSaveRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _productService.UpdateAsync(productId, request, userId, cancellationToken);
        return Ok(ApiResponse<object?>.Ok(null));
    }

    [HttpPut("products/{productId:long}/status")]
    public async Task<ActionResult<ApiResponse<object?>>> SetProductStatus(long productId, [FromBody] StatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _productService.SetStatusAsync(productId, request.Status, userId, cancellationToken);
        return Ok(ApiResponse<object?>.Ok(null));
    }

    [HttpPost("products/{productId:long}/images")]
    public async Task<ActionResult<ApiResponse<long>>> AddProductImage(long productId, [FromBody] ProductImageRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var imageId = await _productImageService.AddAsync(productId, request, userId, cancellationToken);
        return Ok(ApiResponse<long>.Ok(imageId));
    }

    [HttpDelete("product-images/{imageId:long}")]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteProductImage(long imageId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _productImageService.DeleteAsync(imageId, userId, cancellationToken);
        return Ok(ApiResponse<object?>.Ok(null));
    }

    [HttpGet("products/{productId:long}/skus")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SkuDto>>>> Skus(long productId, CancellationToken cancellationToken)
    {
        var skus = await _skuService.GetByProductAsync(productId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SkuDto>>.Ok(skus));
    }

    [HttpGet("skus")]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminSkuListItemDto>>>> SearchSkus([FromQuery] AdminSkuQuery query, CancellationToken cancellationToken)
    {
        var result = await _skuService.SearchAdminAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResult<AdminSkuListItemDto>>.Ok(result));
    }

    [HttpPost("products/{productId:long}/skus")]
    public async Task<ActionResult<ApiResponse<long>>> CreateSku(long productId, [FromBody] SkuSaveRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var skuId = await _skuService.CreateAsync(productId, request, userId, cancellationToken);
        return Ok(ApiResponse<long>.Ok(skuId));
    }

    [HttpPut("skus/{skuId:long}")]
    public async Task<ActionResult<ApiResponse<object?>>> UpdateSku(long skuId, [FromBody] SkuSaveRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _skuService.UpdateAsync(skuId, request, userId, cancellationToken);
        return Ok(ApiResponse<object?>.Ok(null));
    }

    [HttpPut("skus/{skuId:long}/status")]
    public async Task<ActionResult<ApiResponse<object?>>> SetSkuStatus(long skuId, [FromBody] StatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _skuService.SetStatusAsync(skuId, request.Status, userId, cancellationToken);
        return Ok(ApiResponse<object?>.Ok(null));
    }

    [HttpPost("skus/{skuId:long}/inventory-adjustments")]
    public async Task<ActionResult<ApiResponse<object?>>> AdjustInventory(long skuId, [FromBody] InventoryAdjustRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _inventoryService.AdjustAsync(skuId, request, userId, cancellationToken);
        return Ok(ApiResponse<object?>.Ok(null));
    }

    [HttpGet("inventory/warnings")]
    public async Task<ActionResult<ApiResponse<PagedResult<InventoryWarningDto>>>> InventoryWarnings([FromQuery] PageQuery query, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.SearchWarningsAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResult<InventoryWarningDto>>.Ok(result));
    }

    [HttpGet("inventory/logs")]
    public async Task<ActionResult<ApiResponse<PagedResult<InventoryLogDto>>>> InventoryLogs([FromQuery] InventoryLogQuery query, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.SearchLogsAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResult<InventoryLogDto>>.Ok(result));
    }
}
