using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Exceptions;
using Moq;

namespace ECommerce.Tests.Services;

public sealed class UserServiceTests
{
    private const long CurrentAdminId = 10;
    private const long OtherAdminId = 20;
    private const int AdminRoleId = 1;
    private const int UserRoleId = 3;

    [Fact]
    public async Task SetUserStatusAsync_RejectsDisablingCurrentAccount()
    {
        var (service, repository, unitOfWork) = CreateService();
        repository.Setup(item => item.GetByIdAsync(CurrentAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser(CurrentAdminId));

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.SetUserStatusAsync(CurrentAdminId, 0, CurrentAdminId));

        Assert.Equal(ErrorCodes.AuthForbidden, exception.Code);
        repository.Verify(item => item.SetUserStatusAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetUserStatusAsync_RejectsDisablingLastActiveAdmin()
    {
        var (service, repository, unitOfWork) = CreateService();
        repository.Setup(item => item.GetByIdAsync(OtherAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser(OtherAdminId));
        repository.Setup(item => item.GetRoleNamesAsync(OtherAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([AuthConstants.Roles.Admin]);
        repository.Setup(item => item.CountActiveUsersInRoleAsync(AuthConstants.Roles.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.SetUserStatusAsync(OtherAdminId, 0, CurrentAdminId));

        Assert.Equal(ErrorCodes.AuthForbidden, exception.Code);
        repository.Verify(item => item.SetUserStatusAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignRolesAsync_RejectsRemovingCurrentAdminsAdminRole()
    {
        var (service, repository, unitOfWork) = CreateService();
        SetupRoleAssignment(repository, CurrentAdminId);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.AssignRolesAsync(CurrentAdminId, [UserRoleId], CurrentAdminId));

        Assert.Equal(ErrorCodes.AuthForbidden, exception.Code);
        repository.Verify(item => item.ReplaceUserRolesAsync(It.IsAny<long>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignRolesAsync_RejectsRemovingLastActiveAdminsAdminRole()
    {
        var (service, repository, unitOfWork) = CreateService();
        SetupRoleAssignment(repository, OtherAdminId);
        repository.Setup(item => item.CountActiveUsersInRoleAsync(AuthConstants.Roles.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.AssignRolesAsync(OtherAdminId, [UserRoleId], CurrentAdminId));

        Assert.Equal(ErrorCodes.AuthForbidden, exception.Code);
        repository.Verify(item => item.ReplaceUserRolesAsync(It.IsAny<long>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetUserStatusAsync_AllowsDisablingAdminWhenAnotherActiveAdminRemains()
    {
        var (service, repository, unitOfWork) = CreateService();
        repository.Setup(item => item.GetByIdAsync(OtherAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser(OtherAdminId));
        repository.Setup(item => item.GetRoleNamesAsync(OtherAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([AuthConstants.Roles.Admin]);
        repository.Setup(item => item.CountActiveUsersInRoleAsync(AuthConstants.Roles.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        await service.SetUserStatusAsync(OtherAdminId, 0, CurrentAdminId);

        repository.Verify(item => item.SetUserStatusAsync(OtherAdminId, 0, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(item => item.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignRolesAsync_AllowsRemovingAdminWhenAnotherActiveAdminRemains()
    {
        var (service, repository, unitOfWork) = CreateService();
        SetupRoleAssignment(repository, OtherAdminId);
        repository.Setup(item => item.CountActiveUsersInRoleAsync(AuthConstants.Roles.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        await service.AssignRolesAsync(OtherAdminId, [UserRoleId], CurrentAdminId);

        repository.Verify(item => item.ReplaceUserRolesAsync(
            OtherAdminId,
            It.Is<IReadOnlyList<int>>(roles => roles.SequenceEqual(new[] { UserRoleId })),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(item => item.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static void SetupRoleAssignment(Mock<IUserRepository> repository, long userId)
    {
        repository.Setup(item => item.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser(userId));
        repository.Setup(item => item.GetExistingRoleIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([UserRoleId]);
        repository.Setup(item => item.GetRoleIdByNameAsync(AuthConstants.Roles.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminRoleId);
        repository.Setup(item => item.GetRoleNamesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([AuthConstants.Roles.Admin]);
    }

    private static (UserService Service, Mock<IUserRepository> Repository, Mock<IUnitOfWork> UnitOfWork) CreateService()
    {
        var repository = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(item => item.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(item => item.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(item => item.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(item => item.LockRoleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return (new UserService(repository.Object, unitOfWork.Object), repository, unitOfWork);
    }

    private static User CreateUser(long id) => new()
    {
        Id = id,
        Username = $"admin_{id}",
        PasswordHash = "test",
        Status = 1,
        CreatedAt = DateTime.Now
    };
}
