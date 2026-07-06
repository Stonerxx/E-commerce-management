using ECommerce.Application.DTOs;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/admin")]
[Authorize(Policy = AuthConstants.Policies.AdminOnly)]
public sealed class AdminCatalogApiController : ApiControllerBase
{
    [HttpGet("categories")]
    public ActionResult<ApiResponse<IReadOnlyList<CategoryTreeDto>>> Categories()
    {
        return NotReady<IReadOnlyList<CategoryTreeDto>>("Admin category list endpoint is defined and awaiting implementation.");
    }

    [HttpPost("categories")]
    public ActionResult<ApiResponse<int>> CreateCategory([FromBody] CategoryRequest request)
    {
        return NotReady<int>("Admin category create endpoint is defined and awaiting implementation.");
    }

    [HttpPut("categories/{categoryId:int}")]
    public ActionResult<ApiResponse<object?>> UpdateCategory(int categoryId, [FromBody] CategoryRequest request)
    {
        return NotReady<object?>("Admin category update endpoint is defined and awaiting implementation.");
    }

    [HttpDelete("categories/{categoryId:int}")]
    public ActionResult<ApiResponse<object?>> DeleteCategory(int categoryId)
    {
        return NotReady<object?>("Admin category delete endpoint is defined and awaiting implementation.");
    }

    [HttpGet("products")]
    public ActionResult<ApiResponse<PagedResult<ProductListItemDto>>> Products([FromQuery] ProductQuery query)
    {
        return NotReady<PagedResult<ProductListItemDto>>("Admin product search endpoint is defined and awaiting implementation.");
    }

    [HttpPost("products")]
    public ActionResult<ApiResponse<long>> CreateProduct([FromBody] ProductSaveRequest request)
    {
        return NotReady<long>("Admin product create endpoint is defined and awaiting implementation.");
    }

    [HttpPut("products/{productId:long}")]
    public ActionResult<ApiResponse<object?>> UpdateProduct(long productId, [FromBody] ProductSaveRequest request)
    {
        return NotReady<object?>("Admin product update endpoint is defined and awaiting implementation.");
    }

    [HttpPut("products/{productId:long}/status")]
    public ActionResult<ApiResponse<object?>> SetProductStatus(long productId, [FromBody] StatusUpdateRequest request)
    {
        return NotReady<object?>("Admin product status endpoint is defined and awaiting implementation.");
    }

    [HttpPost("products/{productId:long}/images")]
    public ActionResult<ApiResponse<long>> AddProductImage(long productId, [FromBody] ProductImageRequest request)
    {
        return NotReady<long>("Admin product image endpoint is defined and awaiting implementation.");
    }

    [HttpDelete("product-images/{imageId:long}")]
    public ActionResult<ApiResponse<object?>> DeleteProductImage(long imageId)
    {
        return NotReady<object?>("Admin product image delete endpoint is defined and awaiting implementation.");
    }

    [HttpGet("products/{productId:long}/skus")]
    public ActionResult<ApiResponse<IReadOnlyList<SkuDto>>> Skus(long productId)
    {
        return NotReady<IReadOnlyList<SkuDto>>("Admin SKU list endpoint is defined and awaiting implementation.");
    }

    [HttpPost("products/{productId:long}/skus")]
    public ActionResult<ApiResponse<long>> CreateSku(long productId, [FromBody] SkuSaveRequest request)
    {
        return NotReady<long>("Admin SKU create endpoint is defined and awaiting implementation.");
    }

    [HttpPut("skus/{skuId:long}")]
    public ActionResult<ApiResponse<object?>> UpdateSku(long skuId, [FromBody] SkuSaveRequest request)
    {
        return NotReady<object?>("Admin SKU update endpoint is defined and awaiting implementation.");
    }

    [HttpPut("skus/{skuId:long}/status")]
    public ActionResult<ApiResponse<object?>> SetSkuStatus(long skuId, [FromBody] StatusUpdateRequest request)
    {
        return NotReady<object?>("Admin SKU status endpoint is defined and awaiting implementation.");
    }

    [HttpPost("skus/{skuId:long}/inventory-adjustments")]
    public ActionResult<ApiResponse<object?>> AdjustInventory(long skuId, [FromBody] InventoryAdjustRequest request)
    {
        return NotReady<object?>("Inventory adjustment endpoint is defined and awaiting implementation.");
    }

    [HttpGet("inventory/warnings")]
    public ActionResult<ApiResponse<PagedResult<InventoryWarningDto>>> InventoryWarnings([FromQuery] PageQuery query)
    {
        return NotReady<PagedResult<InventoryWarningDto>>("Inventory warning endpoint is defined and awaiting implementation.");
    }

    [HttpGet("inventory/logs")]
    public ActionResult<ApiResponse<PagedResult<InventoryLogDto>>> InventoryLogs([FromQuery] InventoryLogQuery query)
    {
        return NotReady<PagedResult<InventoryLogDto>>("Inventory log endpoint is defined and awaiting implementation.");
    }
}
