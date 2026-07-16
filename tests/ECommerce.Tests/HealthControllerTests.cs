using ECommerce.Infrastructure.Data;
using ECommerce.Shared.Contracts;
using ECommerce.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ECommerce.Tests;

public sealed class HealthControllerTests
{
    [Fact]
    public async Task Ready_WhenDatabaseIsReachable_ShouldReturnOk()
    {
        var controller = CreateController(new DatabaseCheckResult(true, true, "Oracle", null, null, null, null, 1, DateTimeOffset.UtcNow, null));

        var result = await controller.Ready(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Ready_WhenDatabaseIsUnavailable_ShouldReturnServiceUnavailable()
    {
        var controller = CreateController(new DatabaseCheckResult(false, true, "Oracle", null, null, null, null, 1, DateTimeOffset.UtcNow, "offline"));

        var result = await controller.Ready(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
        var response = Assert.IsType<ApiResponse<DatabaseCheckResult>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal("DATABASE_UNAVAILABLE", response.Code);
    }

    [Fact]
    public void Live_ShouldNotRequireDatabaseConnection()
    {
        var healthCheck = new Mock<IDatabaseHealthCheck>(MockBehavior.Strict);
        var controller = CreateController(healthCheck.Object);

        var result = controller.Live();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    private static HealthController CreateController(DatabaseCheckResult checkResult)
    {
        var healthCheck = new Mock<IDatabaseHealthCheck>();
        healthCheck.Setup(x => x.CheckAsync(It.IsAny<CancellationToken>())).ReturnsAsync(checkResult);
        return CreateController(healthCheck.Object);
    }

    private static HealthController CreateController(IDatabaseHealthCheck healthCheck)
    {
        return new HealthController(healthCheck)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
}
