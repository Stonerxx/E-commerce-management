using ECommerce.Shared.Contracts;
using ECommerce.Shared.Errors;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers.Api;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<ApiResponse<T>> NotReady<T>(string message)
    {
        var response = ApiResponse<T>.Fail(
            ErrorCodes.NotImplemented,
            message,
            HttpContext.TraceIdentifier);

        return StatusCode(StatusCodes.Status501NotImplemented, response);
    }
}
