using ECommerce.Shared.Constants;
using ECommerce.Shared.Contracts;
using Xunit;

namespace ECommerce.Tests;

public sealed class ContractTests
{
    [Fact]
    public void ApiResponse_Ok_ShouldUseStandardCode()
    {
        var response = ApiResponse<string>.Ok("pong", "trace-id");

        Assert.True(response.Success);
        Assert.Equal("OK", response.Code);
        Assert.Equal("pong", response.Data);
        Assert.Equal("trace-id", response.TraceId);
    }

    [Fact]
    public void AuthConstants_ShouldKeepRequiredRoleCodes()
    {
        Assert.Equal("USER", AuthConstants.Roles.User);
        Assert.Equal("SERVICE", AuthConstants.Roles.Service);
        Assert.Equal("ADMIN", AuthConstants.Roles.Admin);
    }
}
