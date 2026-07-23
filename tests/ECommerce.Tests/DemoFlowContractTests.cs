using ECommerce.Shared.Constants;
using ECommerce.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ECommerce.Tests;

public sealed class DemoFlowContractTests
{
    [Fact]
    public void PaymentController_ShouldBeCustomerOnlyAndProtectPaymentPost()
    {
        AssertAuthorizePolicy<PaymentController>(AuthConstants.Policies.CustomerOnly);
        AssertHttpRoute<PaymentController>(nameof(PaymentController.Detail), typeof(HttpGetAttribute), "/payment/{orderId:long}");
        AssertHttpRoute<PaymentController>(nameof(PaymentController.SimulatePay), typeof(HttpPostAttribute), "/payment/{orderId:long}/demo-pay");
        AssertHasAttribute<PaymentController, ValidateAntiForgeryTokenAttribute>(nameof(PaymentController.SimulatePay));
    }

    [Fact]
    public void OrdersCreate_ShouldBindRepeatedCartItemIds()
    {
        var method = typeof(OrdersController).GetMethod(nameof(OrdersController.Create));

        Assert.NotNull(method);
        var parameter = Assert.Single(method.GetParameters());
        Assert.Equal(typeof(long[]), parameter.ParameterType);
        Assert.Contains(parameter.GetCustomAttributes(typeof(FromQueryAttribute), false), attribute => attribute is FromQueryAttribute);
    }

    [Fact]
    public void CheckoutEntrypoints_ShouldPreserveCartAndAddressPrerequisites()
    {
        var productDetailScript = ReadProjectFile("src", "ECommerce.Web", "wwwroot", "js", "product-detail.js");
        var checkoutScript = ReadProjectFile("src", "ECommerce.Web", "wwwroot", "js", "order-create.js");
        var couponView = ReadProjectFile("src", "ECommerce.Web", "Views", "Coupons", "Index.cshtml");
        var checkoutView = ReadProjectFile("src", "ECommerce.Web", "Views", "Orders", "Create.cshtml");

        Assert.Contains("fetch('/api/v1/addresses'", productDetailScript);
        Assert.Contains("href=\"/cart\">查看购物车", couponView);
        Assert.DoesNotContain("href=\"/orders/create\">去结算", couponView);
        Assert.Contains("/validate`,", checkoutScript);
        Assert.Contains("result.data.available", checkoutScript);
        Assert.Contains("请先添加收货地址", checkoutView);
        Assert.Contains("coupon.discountAmount", checkoutView);
        Assert.DoesNotContain("购物车为空或数据加载失败", checkoutView);
    }

    [Fact]
    public void InventorySearch_ShouldBeServerSideInsteadOfFilteringOnlyCurrentPage()
    {
        var warningScript = ReadProjectFile("src", "ECommerce.Web", "wwwroot", "js", "admin-inventory-warnings.js");
        var logScript = ReadProjectFile("src", "ECommerce.Web", "wwwroot", "js", "admin-inventory-logs.js");
        var logRepository = ReadProjectFile("src", "ECommerce.Infrastructure", "Repositories", "InventoryLogRepository.cs");

        Assert.Contains("params.set('keyword'", warningScript);
        Assert.Contains("params.set('keyword'", logScript);
        Assert.DoesNotContain("list = list.filter", warningScript);
        Assert.DoesNotContain("list = list.filter", logScript);
        Assert.Contains("query.Keyword.Trim().ToLowerInvariant()", logRepository);
    }

    [Fact]
    public void DocsController_ShouldExposeDemoFlowDocument()
    {
        AssertHttpRoute<DocsController>(nameof(DocsController.DemoFlow), typeof(HttpGetAttribute), "/docs/demo-flow");
    }

    private static void AssertAuthorizePolicy<TController>(string expectedPolicy)
    {
        var attribute = typeof(TController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(expectedPolicy, attribute.Policy);
    }

    private static void AssertHttpRoute<TController>(string actionName, Type httpMethodAttributeType, string expectedTemplate)
    {
        var method = typeof(TController).GetMethod(actionName);

        Assert.NotNull(method);
        var attribute = method
            .GetCustomAttributes(httpMethodAttributeType, false)
            .Cast<HttpMethodAttribute>()
            .Single();

        Assert.Equal(expectedTemplate, attribute.Template);
    }

    private static void AssertHasAttribute<TController, TAttribute>(string actionName)
        where TAttribute : Attribute
    {
        var method = typeof(TController).GetMethod(actionName);

        Assert.NotNull(method);
        Assert.Contains(method.GetCustomAttributes(typeof(TAttribute), false), attribute => attribute is TAttribute);
    }

    private static string ReadProjectFile(params string[] pathSegments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ECommerce.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine([directory.FullName, .. pathSegments]));
    }
}
