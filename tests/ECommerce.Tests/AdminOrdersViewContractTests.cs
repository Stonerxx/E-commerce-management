namespace ECommerce.Tests;

public sealed class AdminOrdersViewContractTests
{
    [Fact]
    public void OrderList_ConnectsShipmentButtonToARealForm()
    {
        var view = ReadRepositoryFile("src", "ECommerce.Web", "Views", "AdminOrders", "Index.cshtml");
        var script = ReadRepositoryFile("src", "ECommerce.Web", "wwwroot", "js", "admin-orders.js");

        Assert.Contains("v-on:click=\"shipOrder(order.orderId)\"", view);
        Assert.Contains("ref=\"shipModalElement\"", view);
        Assert.Contains("v-on:submit.prevent=\"submitShipment\"", view);
        Assert.Contains("this.shipModal?.show()", script);
    }

    [Fact]
    public void OrderList_ExposesPromisedTimeFilters()
    {
        var view = ReadRepositoryFile("src", "ECommerce.Web", "Views", "AdminOrders", "Index.cshtml");

        Assert.Contains("v-model=\"filters.startTime\"", view);
        Assert.Contains("v-model=\"filters.endTime\"", view);
    }

    private static string ReadRepositoryFile(params string[] pathSegments)
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
