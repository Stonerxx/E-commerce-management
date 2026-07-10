using ECommerce.Application.DTOs;
using ECommerce.Shared.Constants;
using ECommerce.Web.Controllers.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ECommerce.Tests;

public sealed class ApiContractTests
{
    [Fact]
    public void ProductDtos_ShouldUseIntCategoryId()
    {
        Assert.Equal(typeof(int), PropertyType<ProductSaveRequest>(nameof(ProductSaveRequest.CategoryId)));
        Assert.Equal(typeof(int?), PropertyType<ProductQuery>(nameof(ProductQuery.CategoryId)));
        Assert.Equal(typeof(int), PropertyType<ProductListItemDto>(nameof(ProductListItemDto.CategoryId)));
        Assert.Equal(typeof(int), PropertyType<ProductDetailDto>(nameof(ProductDetailDto.CategoryId)));
    }

    [Fact]
    public void CouponValidate_ShouldReadOrderAmountFromBodyRequest()
    {
        var method = typeof(CouponsApiController).GetMethod(nameof(CouponsApiController.Validate));

        Assert.NotNull(method);

        var parameters = method.GetParameters();
        Assert.Equal(typeof(CouponValidationRequest), parameters[1].ParameterType);
        Assert.Contains(parameters[1].GetCustomAttributes(typeof(FromBodyAttribute), false), attribute => attribute is FromBodyAttribute);
    }

    [Fact]
    public void AuthController_ShouldRequireLoginExceptRegisterAndLogin()
    {
        AssertAuthorizePolicy<AuthApiController>(AuthConstants.Policies.CustomerOnly);
        AssertAllowAnonymous<AuthApiController>(nameof(AuthApiController.Register));
        AssertAllowAnonymous<AuthApiController>(nameof(AuthApiController.Login));
    }

    [Fact]
    public void AdminStatistics_ShouldUseActionLevelPolicies()
    {
        AssertAuthorizePolicy<AdminStatisticsApiController>(
            nameof(AdminStatisticsApiController.DashboardSummaryAsync),
            AuthConstants.Policies.ServiceOrAdmin);

        AssertAuthorizePolicy<AdminStatisticsApiController>(
            nameof(AdminStatisticsApiController.OrderStatisticsAsync),
            AuthConstants.Policies.AdminOnly);

        AssertAuthorizePolicy<AdminStatisticsApiController>(
            nameof(AdminStatisticsApiController.TopProductsAsync),
            AuthConstants.Policies.AdminOnly);

        AssertAuthorizePolicy<AdminStatisticsApiController>(
            nameof(AdminStatisticsApiController.ExportOrdersAsync),
            AuthConstants.Policies.AdminOnly);

        AssertAuthorizePolicy<AdminStatisticsApiController>(
            nameof(AdminStatisticsApiController.ExportInventoryAsync),
            AuthConstants.Policies.AdminOnly);
    }

    private static Type PropertyType<T>(string propertyName)
    {
        return typeof(T).GetProperty(propertyName)?.PropertyType
            ?? throw new InvalidOperationException($"{typeof(T).Name}.{propertyName} was not found.");
    }

    private static void AssertAuthorizePolicy<TController>(string expectedPolicy)
    {
        var attribute = typeof(TController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(expectedPolicy, attribute.Policy);
    }

    private static void AssertAuthorizePolicy<TController>(string actionName, string expectedPolicy)
    {
        var method = typeof(TController).GetMethod(actionName);

        Assert.NotNull(method);

        var attribute = method
            .GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(expectedPolicy, attribute.Policy);
    }

    private static void AssertAllowAnonymous<TController>(string actionName)
    {
        var method = typeof(TController).GetMethod(actionName);

        Assert.NotNull(method);
        Assert.Contains(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), false), attribute => attribute is AllowAnonymousAttribute);
    }
}
