using ECommerce.Application.DTOs;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1")]
[AllowAnonymous]
public sealed class CatalogApiController : ApiControllerBase
{
    [HttpGet("categories")]
    public ActionResult<ApiResponse<IReadOnlyList<CategoryTreeDto>>> Categories()
    {
        return NotReady<IReadOnlyList<CategoryTreeDto>>("Category tree endpoint is defined and awaiting implementation.");
    }

    [HttpGet("products")]
    public ActionResult<ApiResponse<PagedResult<ProductListItemDto>>> Products([FromQuery] ProductQuery query)
    {
        return NotReady<PagedResult<ProductListItemDto>>("Product search endpoint is defined and awaiting implementation.");
    }

    [HttpGet("products/{productId:long}")]
    public ActionResult<ApiResponse<ProductDetailDto>> ProductDetail(long productId)
    {
        return NotReady<ProductDetailDto>("Product detail endpoint is defined and awaiting implementation.");
    }
}
