using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<UserDto>> SearchUsersAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        return _userRepository.SearchUsersAsync(query, cancellationToken);
    }

    public async Task SetUserStatusAsync(long userId, int status, long operatorId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new BusinessException(ErrorCodes.ValidationError, "用户ID必须大于0");
        }

        if (status is not 0 and not 1)
        {
            throw new BusinessException(ErrorCodes.ValidationError, "用户状态只能是0或1");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new BusinessException(ErrorCodes.ResourceNotFound, "用户不存在");
        }

        if (user.Status == status)
        {
            return;
        }

        if (status == 0 && userId == operatorId)
        {
            throw new BusinessException(ErrorCodes.AuthForbidden, "不能禁用当前登录的管理员账号，请使用其他管理员账号操作");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (status == 0)
            {
                var currentRoles = await _userRepository.GetRoleNamesAsync(userId, cancellationToken);
                if (currentRoles.Contains(AuthConstants.Roles.Admin, StringComparer.OrdinalIgnoreCase))
                {
                    await _userRepository.LockRoleAsync(AuthConstants.Roles.Admin, cancellationToken);
                    if (await _userRepository.CountActiveUsersInRoleAsync(AuthConstants.Roles.Admin, cancellationToken) <= 1)
                    {
                        throw new BusinessException(ErrorCodes.AuthForbidden, "不能禁用最后一个有效管理员账号");
                    }
                }
            }

            await _userRepository.SetUserStatusAsync(userId, status, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task AssignRolesAsync(long userId, IReadOnlyList<int> roleIds, long operatorId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new BusinessException(ErrorCodes.ValidationError, "用户ID必须大于0");
        }

        if (roleIds.Count == 0 || roleIds.Any(id => id <= 0))
        {
            throw new BusinessException(ErrorCodes.ValidationError, "角色ID不能为空且必须大于0");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new BusinessException(ErrorCodes.ResourceNotFound, "用户不存在");
        }

        var distinctRoleIds = roleIds.Distinct().ToArray();
        var existingRoleIds = await _userRepository.GetExistingRoleIdsAsync(distinctRoleIds, cancellationToken);
        if (existingRoleIds.Count != distinctRoleIds.Length)
        {
            var missingIds = distinctRoleIds.Except(existingRoleIds);
            throw new BusinessException(ErrorCodes.ResourceNotFound, $"角色不存在：{string.Join(",", missingIds)}");
        }

        var adminRoleId = await _userRepository.GetRoleIdByNameAsync(AuthConstants.Roles.Admin, cancellationToken)
            ?? throw new BusinessException(ErrorCodes.ConfigurationError, "数据库缺少 ADMIN 角色");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var currentRoles = await _userRepository.GetRoleNamesAsync(userId, cancellationToken);
            var removesAdmin = currentRoles.Contains(AuthConstants.Roles.Admin, StringComparer.OrdinalIgnoreCase)
                && !distinctRoleIds.Contains(adminRoleId);
            if (removesAdmin)
            {
                if (userId == operatorId)
                {
                    throw new BusinessException(ErrorCodes.AuthForbidden, "不能移除当前登录账号的 ADMIN 角色，请使用其他管理员账号操作");
                }

                await _userRepository.LockRoleAsync(AuthConstants.Roles.Admin, cancellationToken);
                if (user.Status == 1
                    && await _userRepository.CountActiveUsersInRoleAsync(AuthConstants.Roles.Admin, cancellationToken) <= 1)
                {
                    throw new BusinessException(ErrorCodes.AuthForbidden, "不能移除最后一个有效管理员的 ADMIN 角色");
                }
            }

            await _userRepository.ReplaceUserRolesAsync(userId, distinctRoleIds, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
