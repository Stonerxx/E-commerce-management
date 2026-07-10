using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public sealed class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PermissionService(IPermissionRepository permissionRepository, IUnitOfWork unitOfWork)
    {
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        return _permissionRepository.GetRolesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(string? keyword, CancellationToken cancellationToken = default)
    {
        return _permissionRepository.GetPermissionsAsync(keyword, cancellationToken);
    }

    public async Task<IReadOnlyList<RolePermissionDto>> GetRolePermissionsAsync(int roleId, CancellationToken cancellationToken = default)
    {
        await EnsureRoleExistsAsync(roleId, cancellationToken);
        return await _permissionRepository.GetRolePermissionsAsync(roleId, cancellationToken);
    }

    public async Task BindRolePermissionsAsync(int roleId, IReadOnlyList<int> permissionIds, CancellationToken cancellationToken = default)
    {
        await EnsureRoleExistsAsync(roleId, cancellationToken);

        if (permissionIds.Any(id => id <= 0))
        {
            throw new BusinessException(ErrorCodes.ValidationError, "权限ID必须大于0");
        }

        var distinctPermissionIds = permissionIds.Distinct().ToArray();
        var existingIds = await _permissionRepository.GetExistingPermissionIdsAsync(distinctPermissionIds, cancellationToken);
        if (existingIds.Count != distinctPermissionIds.Length)
        {
            var missingIds = distinctPermissionIds.Except(existingIds);
            throw new BusinessException(ErrorCodes.ResourceNotFound, $"权限不存在：{string.Join(",", missingIds)}");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _permissionRepository.ReplaceRolePermissionsAsync(roleId, distinctPermissionIds, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> CanAccessAsync(
        IReadOnlyList<string> roleNames,
        string requestPath,
        string httpMethod,
        CancellationToken cancellationToken = default)
    {
        if (roleNames.Contains(AuthConstants.Roles.Admin, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        var hasRule = await _permissionRepository.PermissionRuleExistsAsync(requestPath, httpMethod, cancellationToken);
        if (!hasRule)
        {
            // 未配置到 PERMISSION 表的路由继续交给 ASP.NET Core Policy 控制，避免初始化阶段锁死系统。
            return true;
        }

        return await _permissionRepository.HasRolePermissionAsync(roleNames, requestPath, httpMethod, cancellationToken);
    }

    private async Task EnsureRoleExistsAsync(int roleId, CancellationToken cancellationToken)
    {
        if (roleId <= 0)
        {
            throw new BusinessException(ErrorCodes.ValidationError, "角色ID必须大于0");
        }

        if (!await _permissionRepository.RoleExistsAsync(roleId, cancellationToken))
        {
            throw new BusinessException(ErrorCodes.ResourceNotFound, "角色不存在");
        }
    }
}
