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
}
