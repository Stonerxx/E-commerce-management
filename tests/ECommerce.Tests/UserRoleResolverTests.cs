using ECommerce.Shared.Constants;
using ECommerce.Web.Security;

namespace ECommerce.Tests;

public sealed class UserRoleResolverTests
{
    [Theory]
    [InlineData("USER,SERVICE,ADMIN", AuthConstants.Roles.Admin)]
    [InlineData("USER,SERVICE", AuthConstants.Roles.Service)]
    [InlineData("USER", AuthConstants.Roles.User)]
    public void ResolvePrimary_UsesConsistentPrivilegePriority(string roles, string expected)
    {
        Assert.Equal(expected, UserRoleResolver.ResolvePrimary(roles.Split(',')));
    }

    [Theory]
    [InlineData("USER,ADMIN", "/admin")]
    [InlineData("USER,SERVICE", "/admin")]
    [InlineData("USER", "/")]
    public void GetLandingPath_FollowsPrimaryRole(string roles, string expected)
    {
        Assert.Equal(expected, UserRoleResolver.GetLandingPath(roles.Split(',')));
    }
}
