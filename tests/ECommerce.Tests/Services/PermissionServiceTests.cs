using ECommerce.Application.DTOs;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Constants;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services;

public sealed class PermissionServiceTests
{
    [Theory]
    [InlineData("/api/v1/admin/**", "/api/v1/admin/orders/1", true)]
    [InlineData("/api/v1/admin/**", "/api/v1/admin", true)]
    [InlineData("/api/v1/admin/orders/*/shipments", "/api/v1/admin/orders/12/shipments", true)]
    [InlineData("/api/v1/admin/orders/*/cancel", "/api/v1/admin/orders/12/cancel", true)]
    [InlineData("/api/v1/admin/orders/*/shipments", "/api/v1/admin/orders/12/logs/shipments", false)]
    [InlineData("/admin/*", "/admin/orders", true)]
    [InlineData("/admin/*", "/admin/orders/1", false)]
    public void PermissionPathMatcher_ShouldMatchPathSegments(string pattern, string path, bool expected)
    {
        Assert.Equal(expected, PermissionPathMatcher.IsMatch(pattern, path));
    }

    [Fact]
    public async Task CanAccessAsync_BackendPathWithoutRule_ShouldDenyNonAdmin()
    {
        var repository = CreateRepository(allRules: Array.Empty<PermissionDto>(), roleRules: Array.Empty<PermissionDto>());
        var service = CreateService(repository);

        var allowed = await service.CanAccessAsync(new[] { AuthConstants.Roles.Service }, "/api/v1/admin/orders", "GET");

        Assert.False(allowed);
    }

    [Fact]
    public async Task CanAccessAsync_MatchingRoleRule_ShouldAllow()
    {
        var rule = new PermissionDto(1, "SERVICE_ORDERS_GET", "/api/v1/admin/orders/**", "GET", null);
        var repository = CreateRepository(new[] { rule }, new[] { rule });
        var service = CreateService(repository);

        var allowed = await service.CanAccessAsync(new[] { AuthConstants.Roles.Service }, "/api/v1/admin/orders/1", "GET");

        Assert.True(allowed);
    }

    [Fact]
    public async Task CanAccessAsync_NonBackendPathWithoutRule_ShouldDeferToPolicy()
    {
        var repository = CreateRepository(Array.Empty<PermissionDto>(), Array.Empty<PermissionDto>());
        var service = CreateService(repository);

        var allowed = await service.CanAccessAsync(new[] { AuthConstants.Roles.User }, "/api/v1/cart", "GET");

        Assert.True(allowed);
    }

    [Fact]
    public async Task CanAccessAsync_Admin_ShouldBypassDynamicRuleLookup()
    {
        var repository = new Mock<IPermissionRepository>(MockBehavior.Strict);
        var service = CreateService(repository);

        var allowed = await service.CanAccessAsync(new[] { AuthConstants.Roles.Admin }, "/api/v1/admin/users", "DELETE");

        Assert.True(allowed);
    }

    [Fact]
    public async Task BindRolePermissionsAsync_AdminRole_ShouldRejectFakeEditableBindings()
    {
        var repository = new Mock<IPermissionRepository>();
        repository.Setup(item => item.GetRoleNameAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthConstants.Roles.Admin);
        var unitOfWork = new Mock<IUnitOfWork>();
        var service = new PermissionService(repository.Object, unitOfWork.Object);

        var exception = await Assert.ThrowsAsync<ECommerce.Shared.Exceptions.BusinessException>(() =>
            service.BindRolePermissionsAsync(1, [1, 2, 3]));

        Assert.Equal(ECommerce.Shared.Errors.ErrorCodes.AuthForbidden, exception.Code);
        repository.Verify(item => item.ReplaceRolePermissionsAsync(
            It.IsAny<int>(),
            It.IsAny<IReadOnlyList<int>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IPermissionRepository> CreateRepository(
        IReadOnlyList<PermissionDto> allRules,
        IReadOnlyList<PermissionDto> roleRules)
    {
        var repository = new Mock<IPermissionRepository>();
        repository.Setup(x => x.GetPermissionRulesByActionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allRules);
        repository.Setup(x => x.GetRolePermissionRulesByActionAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(roleRules);
        return repository;
    }

    private static PermissionService CreateService(Mock<IPermissionRepository> repository) => new(
        repository.Object,
        new Mock<IUnitOfWork>().Object);
}
