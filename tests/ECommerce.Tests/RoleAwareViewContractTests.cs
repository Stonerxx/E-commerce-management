namespace ECommerce.Tests;

public sealed class RoleAwareViewContractTests
{
    [Fact]
    public void RoleSensitiveViews_UseTheSharedPrimaryRoleResolver()
    {
        var home = ReadRepositoryFile("src", "ECommerce.Web", "Views", "Home", "Index.cshtml");
        var layout = ReadRepositoryFile("src", "ECommerce.Web", "Views", "Shared", "_Layout.cshtml");
        var productDetail = ReadRepositoryFile("src", "ECommerce.Web", "Views", "Products", "Detail.cshtml");

        Assert.Contains("UserRoleResolver.ResolvePrimary(User)", home);
        Assert.Contains("UserRoleResolver.ResolvePrimary(User)", layout);
        Assert.Contains("UserRoleResolver.ResolvePrimary(User)", productDetail);
        Assert.Contains("v-else-if=\"@isBackendUser.ToString().ToLowerInvariant()\"", productDetail);
        Assert.Contains("else if (canUseAdmin)", layout);
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
