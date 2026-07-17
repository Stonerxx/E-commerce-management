using ECommerce.Shared.Constants;
using ECommerce.Web.Controllers;
using ECommerce.Web.Controllers.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ECommerce.Tests;

public sealed class FeatureCompletionContractTests
{
    [Fact]
    public void CouponCenter_IsCustomerOnlyPage()
    {
        Assert.Equal(
            AuthConstants.Policies.CustomerOnly,
            typeof(CouponsController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>().Single().Policy);
        AssertRoute<CouponsController>(nameof(CouponsController.Index), typeof(HttpGetAttribute), "/coupons");
    }

    [Theory]
    [InlineData(nameof(AdminController.Coupons), "/admin/coupons")]
    [InlineData(nameof(AdminController.Reviews), "/admin/reviews")]
    [InlineData(nameof(AdminController.Statistics), "/admin/statistics")]
    public void CompletionAdminPages_AreAdminOnly(string actionName, string route)
    {
        var method = typeof(AdminController).GetMethod(actionName);

        Assert.NotNull(method);
        var authorization = method.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>().Single();
        Assert.Equal(AuthConstants.Policies.AdminOnly, authorization.Policy);
        AssertRoute<AdminController>(actionName, typeof(HttpGetAttribute), route);
    }

    [Fact]
    public void StatisticsPage_UsesItsOwnView()
    {
        var result = new AdminController().Statistics();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Null(view.ViewName);
    }

    [Fact]
    public void AdminLogisticsQuery_IsServiceOrAdmin()
    {
        var method = typeof(LogisticsApiController).GetMethod(nameof(LogisticsApiController.GetByOrderAdmin));

        Assert.NotNull(method);
        var authorization = method.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>().Single();
        Assert.Equal(AuthConstants.Policies.ServiceOrAdmin, authorization.Policy);
        AssertRoute<LogisticsApiController>(
            nameof(LogisticsApiController.GetByOrderAdmin),
            typeof(HttpGetAttribute),
            "admin/orders/{orderId:long}/logistics");
    }

    private static void AssertRoute<TController>(string actionName, Type attributeType, string expectedRoute)
    {
        var method = typeof(TController).GetMethod(actionName);
        Assert.NotNull(method);
        var route = method.GetCustomAttributes(attributeType, false).Cast<HttpMethodAttribute>().Single();
        Assert.Equal(expectedRoute, route.Template);
    }
}
