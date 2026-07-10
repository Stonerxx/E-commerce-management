using System.Data;
using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public UserRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<User?> GetByIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        AttachTransaction(command);
        command.BindByName = true;
        command.CommandText = """
            SELECT id, username, password_hash, phone, email, avatar_url, status, created_at, last_login_at
            FROM "USER"
            WHERE id = :user_id
            """;
        command.Parameters.Add(new OracleParameter("user_id", userId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapUser(reader) : null;
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        AttachTransaction(command);
        command.BindByName = true;
        command.CommandText = """
            SELECT id, username, password_hash, phone, email, avatar_url, status, created_at, last_login_at
            FROM "USER"
            WHERE username = :username
            """;
        command.Parameters.Add(new OracleParameter("username", username));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapUser(reader) : null;
    }

    public async Task<long> CreateUserWithDefaultRoleAsync(User user, string defaultRoleName, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
            await using var roleCommand = connection.CreateCommand();
            roleCommand.BindByName = true;
            AttachTransaction(roleCommand);
            roleCommand.CommandText = """SELECT id FROM "ROLE" WHERE role_name = :role_name""";
            roleCommand.Parameters.Add(new OracleParameter("role_name", defaultRoleName));
            var roleIdValue = await roleCommand.ExecuteScalarAsync(cancellationToken);
            if (roleIdValue is null)
            {
                throw new InvalidOperationException("Default role USER is missing in ROLE table.");
            }

            await using var userCommand = connection.CreateCommand();
            userCommand.BindByName = true;
            AttachTransaction(userCommand);
            userCommand.CommandText = """
                INSERT INTO "USER"(username, password_hash, phone, email, avatar_url, status)
                VALUES (:username, :password_hash, :phone, :email, :avatar_url, :status)
                RETURNING id INTO :id
                """;
            userCommand.Parameters.Add(new OracleParameter("username", user.Username));
            userCommand.Parameters.Add(new OracleParameter("password_hash", user.PasswordHash));
            userCommand.Parameters.Add(new OracleParameter("phone", (object?)user.Phone ?? DBNull.Value));
            userCommand.Parameters.Add(new OracleParameter("email", (object?)user.Email ?? DBNull.Value));
            userCommand.Parameters.Add(new OracleParameter("avatar_url", (object?)user.AvatarUrl ?? DBNull.Value));
            userCommand.Parameters.Add(new OracleParameter("status", user.Status));
            var userIdParameter = new OracleParameter("id", OracleDbType.Int64)
            {
                Direction = ParameterDirection.Output
            };
            userCommand.Parameters.Add(userIdParameter);
            await userCommand.ExecuteNonQueryAsync(cancellationToken);

            var userId = Convert.ToInt64(userIdParameter.Value.ToString());

            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = Convert.ToInt32(roleIdValue)
            };

            await using var userRoleCommand = connection.CreateCommand();
            userRoleCommand.BindByName = true;
            AttachTransaction(userRoleCommand);
            userRoleCommand.CommandText = """
                INSERT INTO USER_ROLE(user_id, role_id)
                VALUES (:user_id, :role_id)
                """;
            userRoleCommand.Parameters.Add(new OracleParameter("user_id", userRole.UserId));
            userRoleCommand.Parameters.Add(new OracleParameter("role_id", userRole.RoleId));
            await userRoleCommand.ExecuteNonQueryAsync(cancellationToken);

            return userId;
    }

    public async Task UpdateLastLoginAsync(long userId, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        AttachTransaction(command);
        command.BindByName = true;
        command.CommandText = """UPDATE "USER" SET last_login_at = SYSDATE WHERE id = :user_id""";
        command.Parameters.Add(new OracleParameter("user_id", userId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(long userId, CancellationToken cancellationToken = default)
    {
        var roles = new List<string>();
        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        AttachTransaction(command);
        command.BindByName = true;
        command.CommandText = """
            SELECT r.role_name
            FROM USER_ROLE ur
            JOIN "ROLE" r ON r.id = ur.role_id
            WHERE ur.user_id = :user_id
            ORDER BY r.id
            """;
        command.Parameters.Add(new OracleParameter("user_id", userId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            roles.Add(Convert.ToString(reader["role_name"]) ?? string.Empty);
        }

        return roles;
    }

    public async Task<IReadOnlyList<int>> GetExistingRoleIdsAsync(IReadOnlyList<int> roleIds, CancellationToken cancellationToken = default)
    {
        if (roleIds.Count == 0)
        {
            return Array.Empty<int>();
        }

        var ids = roleIds.Distinct().ToArray();
        var names = ids.Select((_, index) => $":r{index}").ToArray();
        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        AttachTransaction(command);
        command.BindByName = true;
        command.CommandText = $"""SELECT id FROM "ROLE" WHERE id IN ({string.Join(", ", names)})""";

        for (var i = 0; i < ids.Length; i++)
        {
            command.Parameters.Add(new OracleParameter($"r{i}", ids[i]));
        }

        var result = new List<int>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(Convert.ToInt32(reader["id"]));
        }

        return result;
    }

    public async Task<PagedResult<UserDto>> SearchUsersAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        var pageIndex = query.SafePageIndex;
        var pageSize = query.SafePageSize;
        var offset = (pageIndex - 1) * pageSize;
        var keyword = string.IsNullOrWhiteSpace(query.Keyword) ? null : query.Keyword.Trim();
        var role = string.IsNullOrWhiteSpace(query.Role) ? null : query.Role.Trim();

        var connection = await GetConnectionAsync(cancellationToken);

        await using var countCommand = connection.CreateCommand();
        countCommand.BindByName = true;
        AttachTransaction(countCommand);
        countCommand.CommandText = """
            SELECT COUNT(1)
            FROM "USER" u
            WHERE (:keyword IS NULL OR u.username LIKE :keyword_like OR u.phone LIKE :keyword_like OR u.email LIKE :keyword_like)
              AND (:status IS NULL OR u.status = :status)
              AND (:role_name IS NULL OR EXISTS (
                    SELECT 1
                    FROM USER_ROLE ur
                    JOIN "ROLE" r ON r.id = ur.role_id
                    WHERE ur.user_id = u.id AND r.role_name = :role_name
              ))
            """;
        AddUserQueryParameters(countCommand, keyword, query.Status, role);
        var totalCount = Convert.ToInt64(await countCommand.ExecuteScalarAsync(cancellationToken));

        await using var dataCommand = connection.CreateCommand();
        dataCommand.BindByName = true;
        AttachTransaction(dataCommand);
        dataCommand.CommandText = """
            SELECT u.id,
                   u.username,
                   u.phone,
                   u.email,
                   u.status,
                   u.created_at,
                   u.last_login_at,
                   LISTAGG(r.role_name, ',') WITHIN GROUP (ORDER BY r.id) AS role_names
            FROM "USER" u
            LEFT JOIN USER_ROLE ur ON ur.user_id = u.id
            LEFT JOIN "ROLE" r ON r.id = ur.role_id
            WHERE (:keyword IS NULL OR u.username LIKE :keyword_like OR u.phone LIKE :keyword_like OR u.email LIKE :keyword_like)
              AND (:status IS NULL OR u.status = :status)
              AND (:role_name IS NULL OR EXISTS (
                    SELECT 1
                    FROM USER_ROLE ur2
                    JOIN "ROLE" r2 ON r2.id = ur2.role_id
                    WHERE ur2.user_id = u.id AND r2.role_name = :role_name
              ))
            GROUP BY u.id, u.username, u.phone, u.email, u.status, u.created_at, u.last_login_at
            ORDER BY u.id DESC
            OFFSET :offset ROWS FETCH NEXT :page_size ROWS ONLY
            """;
        AddUserQueryParameters(dataCommand, keyword, query.Status, role);
        dataCommand.Parameters.Add(new OracleParameter("offset", offset));
        dataCommand.Parameters.Add(new OracleParameter("page_size", pageSize));

        var items = new List<UserDto>();
        await using var reader = await dataCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var roleNames = reader["role_names"] == DBNull.Value
                ? Array.Empty<string>()
                : Convert.ToString(reader["role_names"])!.Split(',', StringSplitOptions.RemoveEmptyEntries);

            items.Add(new UserDto(
                Convert.ToInt64(reader["id"]),
                Convert.ToString(reader["username"]) ?? string.Empty,
                reader["phone"] == DBNull.Value ? null : Convert.ToString(reader["phone"]),
                reader["email"] == DBNull.Value ? null : Convert.ToString(reader["email"]),
                Convert.ToInt32(reader["status"]),
                Convert.ToDateTime(reader["created_at"]),
                reader["last_login_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["last_login_at"]),
                roleNames));
        }

        return new PagedResult<UserDto>(items, pageIndex, pageSize, totalCount);
    }

    public async Task SetUserStatusAsync(long userId, int status, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        AttachTransaction(command);
        command.BindByName = true;
        command.CommandText = """UPDATE "USER" SET status = :status WHERE id = :user_id""";
        command.Parameters.Add(new OracleParameter("status", status));
        command.Parameters.Add(new OracleParameter("user_id", userId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ReplaceUserRolesAsync(long userId, IReadOnlyList<int> roleIds, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
            await using var deleteCommand = connection.CreateCommand();
            deleteCommand.BindByName = true;
            AttachTransaction(deleteCommand);
            deleteCommand.CommandText = """DELETE FROM USER_ROLE WHERE user_id = :user_id""";
            deleteCommand.Parameters.Add(new OracleParameter("user_id", userId));
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

            foreach (var roleId in roleIds.Distinct())
            {
                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = roleId
                };

                await using var insertCommand = connection.CreateCommand();
                insertCommand.BindByName = true;
                AttachTransaction(insertCommand);
                insertCommand.CommandText = """
                    INSERT INTO USER_ROLE(user_id, role_id)
                    VALUES (:user_id, :role_id)
                    """;
                insertCommand.Parameters.Add(new OracleParameter("user_id", userRole.UserId));
                insertCommand.Parameters.Add(new OracleParameter("role_id", userRole.RoleId));
                await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            }
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

    private static void AddUserQueryParameters(OracleCommand command, string? keyword, int? status, string? role)
    {
        command.Parameters.Add(new OracleParameter("keyword", (object?)keyword ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("keyword_like", keyword is null ? DBNull.Value : $"%{keyword}%"));
        command.Parameters.Add(new OracleParameter("status", (object?)status ?? DBNull.Value));
        command.Parameters.Add(new OracleParameter("role_name", (object?)role ?? DBNull.Value));
    }

    private static User MapUser(IDataRecord reader)
    {
        return new User
        {
            Id = Convert.ToInt64(reader["id"]),
            Username = Convert.ToString(reader["username"]) ?? string.Empty,
            PasswordHash = Convert.ToString(reader["password_hash"]) ?? string.Empty,
            Phone = reader["phone"] == DBNull.Value ? null : Convert.ToString(reader["phone"]),
            Email = reader["email"] == DBNull.Value ? null : Convert.ToString(reader["email"]),
            AvatarUrl = reader["avatar_url"] == DBNull.Value ? null : Convert.ToString(reader["avatar_url"]),
            Status = Convert.ToInt32(reader["status"]),
            CreatedAt = Convert.ToDateTime(reader["created_at"]),
            LastLoginAt = reader["last_login_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["last_login_at"])
        };
    }
}
