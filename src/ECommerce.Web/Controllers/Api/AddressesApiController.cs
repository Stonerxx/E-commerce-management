using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[Route("api/v1/addresses")]
[Authorize(Policy = AuthConstants.Policies.CustomerOnly)]
public sealed class AddressesApiController : ApiControllerBase
{
    private readonly IAddressService _addressService;

    public AddressesApiController(IAddressService addressService)
    {
        _addressService = addressService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AddressDto>>>> GetMine(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var addresses = await _addressService.GetMyAddressesAsync(userId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AddressDto>>.Ok(addresses));
    }

    [HttpPost]
    public ActionResult<ApiResponse<long>> Create([FromBody] AddressRequest request)
    {
        return NotReady<long>("Address create endpoint is defined and awaiting implementation.");
    }

    [HttpPut("{addressId:long}")]
    public ActionResult<ApiResponse<object?>> Update(long addressId, [FromBody] AddressRequest request)
    {
        return NotReady<object?>("Address update endpoint is defined and awaiting implementation.");
    }

    [HttpDelete("{addressId:long}")]
    public ActionResult<ApiResponse<object?>> Delete(long addressId)
    {
        return NotReady<object?>("Address delete endpoint is defined and awaiting implementation.");
    }

    [HttpPut("{addressId:long}/default")]
    public ActionResult<ApiResponse<object?>> SetDefault(long addressId)
    {
        return NotReady<object?>("Address default endpoint is defined and awaiting implementation.");
    }
}
