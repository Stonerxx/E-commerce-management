using System.Data;
using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Shared.Abstractions;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Infrastructure.Repositories;

public sealed class PermissionRepository : IPermissionRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public PermissionRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = new List<Role>();
        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        AttachTransaction(command);
        command.CommandText = """
            SELECT id, role_name, description, created_at
            FROM "ROLE"
            ORDER BY id
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            roles.Add(MapRole(reader));
        }

        return roles
            .Select(role => new RoleDto(role.Id, role.RoleName, role.Description, role.CreatedAt))
            .ToArray();
    }

    public async Task<bool> RoleExistsAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        AttachTransaction(command);
        command.CommandText = """SELECT COUNT(1) FROM "ROLE" WHERE id = :role_id""";
        command.Parameters.Add(new OracleParameter("role_id", roleId));

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(string? keyword, CancellationToken cancellationToken = default)
    {
        var permissions = new List<Permission>();
        var normalizedKeyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();

        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        AttachTransaction(command);
        command.CommandText = """
            SELECT id, perm_name, resource_path, action, description
            FROM PERMISSION
            WHERE (:keyword IS NULL
                OR perm_name LIKE :keyword_like
                OR resource_path LIKE :keyword_like
                OR action LIKE :keyword_like)
            ORDER BY id
            """;
        command.Parameters.Add(new OracleParameter("keyword", (object?)normalizedKeyword ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("keyword_like", normalizedKeyword is null ? DBNull.Value : $"%{normalizedKeyword}%"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            permissions.Add(MapPermission(reader));
        }

        return permissions
            .Select(permission => new PermissionDto(
                permission.Id,
                permission.PermName,
                permission.ResourcePath,
                permission.Action,
                permission.Description))
            .ToArray();
    }

    public async Task<IReadOnlyList<int>> GetExistingPermissionIdsAsync(IReadOnlyList<int> permissionIds, CancellationToken cancellationToken = default)
    {
        if (permissionIds.Count == 0)
        {
            return Array.Empty<int>();
        }

        var ids = permissionIds.Distinct().ToArray();
        var names = ids.Select((_, index) => $":p{index}").ToArray();
        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        AttachTransaction(command);
        command.CommandText = $"""SELECT id FROM PERMISSION WHERE id IN ({string.Join(", ", names)})""";
        for (var i = 0; i < ids.Length; i++)
        {
            command.Parameters.Add(new OracleParameter($"p{i}", ids[i]));
        }

        var result = new List<int>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(Convert.ToInt32(reader["id"]));
        }

        return result;
    }

    public async Task<IReadOnlyList<RolePermissionDto>> GetRolePermissionsAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var items = new List<(Role Role, Permission Permission, RolePermission RolePermission)>();
        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        AttachTransaction(command);
        command.CommandText = """
            SELECT rp.id AS role_permission_id,
                   r.id AS role_id,
                   r.role_name,
                   p.id AS permission_id,
                   p.perm_name,
                   p.resource_path,
                   p.action
            FROM "ROLE" r
            JOIN ROLE_PERMISSION rp ON rp.role_id = r.id
            JOIN PERMISSION p ON p.id = rp.permission_id
            WHERE r.id = :role_id
            ORDER BY p.id
            """;
        command.Parameters.Add(new OracleParameter("role_id", roleId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var role = new Role
            {
                Id = Convert.ToInt32(reader["role_id"]),
                RoleName = Convert.ToString(reader["role_name"]) ?? string.Empty
            };
            var permission = MapPermission(reader, "permission_id");
            var rolePermission = new RolePermission
            {
                Id = Convert.ToInt32(reader["role_permission_id"]),
                RoleId = role.Id,
                PermissionId = permission.Id
            };

            items.Add((role, permission, rolePermission));
        }

        return items
            .Select(item => new RolePermissionDto(
                item.RolePermission.RoleId,
                item.Role.RoleName,
                item.RolePermission.PermissionId,
                item.Permission.PermName,
                item.Permission.ResourcePath,
                item.Permission.Action))
            .ToArray();
    }

    public async Task ReplaceRolePermissionsAsync(int roleId, IReadOnlyList<int> permissionIds, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await using var deleteCommand = connection.CreateCommand();
        deleteCommand.BindByName = true;
        AttachTransaction(deleteCommand);
        deleteCommand.CommandText = """DELETE FROM ROLE_PERMISSION WHERE role_id = :role_id""";
        deleteCommand.Parameters.Add(new OracleParameter("role_id", roleId));
        await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

        foreach (var permissionId in permissionIds.Distinct())
        {
            var rolePermission = new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            };

            await using var insertCommand = connection.CreateCommand();
            insertCommand.BindByName = true;
            AttachTransaction(insertCommand);
            insertCommand.CommandText = """
                INSERT INTO ROLE_PERMISSION(role_id, permission_id)
                VALUES (:role_id, :permission_id)
                """;
            insertCommand.Parameters.Add(new OracleParameter("role_id", rolePermission.RoleId));
            insertCommand.Parameters.Add(new OracleParameter("permission_id", rolePermission.PermissionId));
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<bool> PermissionRuleExistsAsync(string requestPath, string httpMethod, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        AttachTransaction(command);
        command.CommandText = $"""
            SELECT COUNT(1)
            FROM PERMISSION p
            WHERE {BuildPermissionMatchCondition("p")}
            """;
        AddPermissionMatchParameters(command, requestPath, httpMethod);

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<bool> HasRolePermissionAsync(
        IReadOnlyList<string> roleNames,
        string requestPath,
        string httpMethod,
        CancellationToken cancellationToken = default)
    {
        var normalizedRoles = roleNames
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedRoles.Length == 0)
        {
            return false;
        }

        var roleParameters = normalizedRoles.Select((_, index) => $":role{index}").ToArray();
        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        AttachTransaction(command);
        command.CommandText = $"""
            SELECT COUNT(1)
            FROM "ROLE" r
            JOIN ROLE_PERMISSION rp ON rp.role_id = r.id
            JOIN PERMISSION p ON p.id = rp.permission_id
            WHERE r.role_name IN ({string.Join(", ", roleParameters)})
              AND {BuildPermissionMatchCondition("p")}
            """;

        for (var i = 0; i < normalizedRoles.Length; i++)
        {
            command.Parameters.Add(new OracleParameter($"role{i}", normalizedRoles[i]));
        }

        AddPermissionMatchParameters(command, requestPath, httpMethod);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private async Task<OracleConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        return (OracleConnection)await _unitOfWork.GetOpenConnectionAsync(cancellationToken);
    }

    private void AttachTransaction(OracleCommand command)
    {
        if (_unitOfWork.CurrentTransaction is not null)
        {
            command.Transaction = (OracleTransaction)_unitOfWork.CurrentTransaction;
        }
    }

    private static Role MapRole(IDataRecord reader)
    {
        return new Role
        {
            Id = Convert.ToInt32(reader["id"]),
            RoleName = Convert.ToString(reader["role_name"]) ?? string.Empty,
            Description = reader["description"] == DBNull.Value ? null : Convert.ToString(reader["description"]),
            CreatedAt = Convert.ToDateTime(reader["created_at"])
        };
    }

    private static Permission MapPermission(IDataRecord reader, string idColumn = "id")
    {
        return new Permission
        {
            Id = Convert.ToInt32(reader[idColumn]),
            PermName = Convert.ToString(reader["perm_name"]) ?? string.Empty,
            ResourcePath = reader["resource_path"] == DBNull.Value ? null : Convert.ToString(reader["resource_path"]),
            Action = reader["action"] == DBNull.Value ? null : Convert.ToString(reader["action"]),
            Description = HasColumn(reader, "description") && reader["description"] != DBNull.Value
                ? Convert.ToString(reader["description"])
                : null
        };
    }

    private static bool HasColumn(IDataRecord reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string BuildPermissionMatchCondition(string alias)
    {
        return $"""
               (
                   LOWER(:request_path) = LOWER({alias}.resource_path)
                   OR ({alias}.resource_path LIKE '%/**' AND LOWER(:request_path) LIKE LOWER(REPLACE({alias}.resource_path, '/**', '%')))
                   OR ({alias}.resource_path LIKE '%/*' AND LOWER(:request_path) LIKE LOWER(REPLACE({alias}.resource_path, '/*', '/%')))
               )
               AND (
                   {alias}.action IS NULL
                   OR UPPER({alias}.action) = UPPER(:http_method)
                   OR {alias}.action = :action_name
               )
               """;
    }

    private static void AddPermissionMatchParameters(OracleCommand command, string requestPath, string httpMethod)
    {
        command.Parameters.Add(new OracleParameter("request_path", requestPath));
        command.Parameters.Add(new OracleParameter("http_method", httpMethod));
        command.Parameters.Add(new OracleParameter("action_name", ToActionName(httpMethod)));
    }

    private static string ToActionName(string httpMethod)
    {
        return httpMethod.ToUpperInvariant() switch
        {
            "GET" => "查询",
            "POST" => "新增",
            "PUT" => "修改",
            "PATCH" => "修改",
            "DELETE" => "删除",
            _ => httpMethod
        };
    }
}
