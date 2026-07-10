using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using ECommerce.Web.Extensions;
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
        var addresses = await _addressService.GetMyAddressesAsync(User.GetUserId(), cancellationToken);
        return ApiResponse<IReadOnlyList<AddressDto>>.Ok(addresses, HttpContext.TraceIdentifier);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<long>>> Create(
        [FromBody] AddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var addressId = await _addressService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return ApiResponse<long>.Ok(addressId, HttpContext.TraceIdentifier, "收货地址新增成功");
    }

    [HttpPut("{addressId:long}")]
    public async Task<ActionResult<ApiResponse<object?>>> Update(
        long addressId,
        [FromBody] AddressRequest request,
        CancellationToken cancellationToken = default)
    {
        await _addressService.UpdateAsync(User.GetUserId(), addressId, request, cancellationToken);
        return ApiResponse<object?>.Ok(null, HttpContext.TraceIdentifier, "收货地址修改成功");
    }

    [HttpDelete("{addressId:long}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(long addressId, CancellationToken cancellationToken = default)
    {
        await _addressService.DeleteAsync(User.GetUserId(), addressId, cancellationToken);
        return ApiResponse<object?>.Ok(null, HttpContext.TraceIdentifier, "收货地址删除成功");
    }

    [HttpPut("{addressId:long}/default")]
    public async Task<ActionResult<ApiResponse<object?>>> SetDefault(long addressId, CancellationToken cancellationToken = default)
    {
        await _addressService.SetDefaultAsync(User.GetUserId(), addressId, cancellationToken);
        return ApiResponse<object?>.Ok(null, HttpContext.TraceIdentifier, "默认收货地址设置成功");
    }
}
