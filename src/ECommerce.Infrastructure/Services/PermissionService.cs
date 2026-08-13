using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Exceptions;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerce.Infrastructure.Services;

public sealed class PermissionService : IPermissionService
{
    private static readonly TimeSpan AllRulesCacheDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RoleRulesCacheDuration = TimeSpan.FromMinutes(1);
    private static readonly string[] SupportedActions = ["GET", "POST", "PUT", "DELETE"];

    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;

    public PermissionService(
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork,
        IMemoryCache cache)
    {
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
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
        if (roleId <= 0)
        {
            throw new BusinessException(ErrorCodes.ValidationError, "角色ID必须大于0");
        }

        var roleName = await _permissionRepository.GetRoleNameAsync(roleId, cancellationToken);
        if (roleName is null)
        {
            throw new BusinessException(ErrorCodes.ResourceNotFound, "角色不存在");
        }

        if (string.Equals(roleName, AuthConstants.Roles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(ErrorCodes.AuthForbidden, "ADMIN 是内置超级管理员，始终拥有完整后台权限，不能修改其权限绑定");
        }

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
            InvalidateRoleRules(roleName);
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

        var action = httpMethod.ToUpperInvariant();
        var allRules = await _cache.GetOrCreateAsync(
            GetAllRulesCacheKey(action),
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = AllRulesCacheDuration;
                return _permissionRepository.GetPermissionRulesByActionAsync(action, cancellationToken);
            }) ?? Array.Empty<PermissionDto>();

        var matchingRules = allRules
            .Where(rule => PermissionPathMatcher.IsMatch(rule.ResourcePath, requestPath))
            .ToArray();

        if (matchingRules.Length == 0)
        {
            return !IsStrictBackendPath(requestPath);
        }

        foreach (var roleName in roleNames
                     .Where(role => !string.IsNullOrWhiteSpace(role))
                     .Select(role => role.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var roleRules = await _cache.GetOrCreateAsync(
                GetRoleRulesCacheKey(roleName, action),
                entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = RoleRulesCacheDuration;
                    return _permissionRepository.GetRolePermissionRulesByActionAsync(
                        [roleName],
                        action,
                        cancellationToken);
                }) ?? Array.Empty<PermissionDto>();

            if (roleRules.Any(rule => PermissionPathMatcher.IsMatch(rule.ResourcePath, requestPath)))
            {
                return true;
            }
        }

        return false;
    }

    private void InvalidateRoleRules(string roleName)
    {
        foreach (var action in SupportedActions)
        {
            _cache.Remove(GetRoleRulesCacheKey(roleName, action));
        }
    }

    private static string GetAllRulesCacheKey(string action) => $"rbac:all:{action}";

    private static string GetRoleRulesCacheKey(string roleName, string action) =>
        $"rbac:role:{roleName.ToUpperInvariant()}:{action}";

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

    private static bool IsStrictBackendPath(string requestPath)
    {
        var normalizedPath = PermissionPathMatcher.NormalizePath(requestPath);
        return normalizedPath.Equals("/admin", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.Equals("/api/v1/admin", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.StartsWith("/api/v1/admin/", StringComparison.OrdinalIgnoreCase);
    }
}
