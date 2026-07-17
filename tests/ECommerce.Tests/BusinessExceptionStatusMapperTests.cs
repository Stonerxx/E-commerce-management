using ECommerce.Shared.Errors;
using ECommerce.Web.Errors;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Tests;

public sealed class BusinessExceptionStatusMapperTests
{
    [Theory]
    [InlineData("PRODUCT_NOT_FOUND", StatusCodes.Status404NotFound)]
    [InlineData("FORBIDDEN", StatusCodes.Status403Forbidden)]
    [InlineData(ErrorCodes.AuthInvalidCredentials, StatusCodes.Status401Unauthorized)]
    [InlineData("ORDER_STATUS_CHANGED", StatusCodes.Status409Conflict)]
    [InlineData("ALREADY_REVIEWED", StatusCodes.Status409Conflict)]
    [InlineData("LOGISTICS_ALREADY_EXISTS", StatusCodes.Status409Conflict)]
    [InlineData(ErrorCodes.ValidationError, StatusCodes.Status400BadRequest)]
    public void GetStatusCode_MapsBusinessCodesToHttpSemantics(string code, int expectedStatus)
    {
        Assert.Equal(expectedStatus, BusinessExceptionStatusMapper.GetStatusCode(code));
    }
}
