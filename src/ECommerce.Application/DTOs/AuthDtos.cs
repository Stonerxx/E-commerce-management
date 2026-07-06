using ECommerce.Shared.Contracts;

namespace ECommerce.Application.DTOs;

public sealed record RegisterRequest(
    string Username,
    string Password,
    string? Phone,
    string? Email);

public sealed record LoginRequest(
    string Username,
    string Password,
    bool RememberMe);

public sealed record UserSessionDto(
    long UserId,
    string Username,
    IReadOnlyList<string> Roles);

public sealed record UserDto(
    long UserId,
    string Username,
    string? Phone,
    string? Email,
    int Status,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    IReadOnlyList<string> Roles);

public sealed record UserQuery : PageQuery
{
    public string? Keyword { get; init; }

    public int? Status { get; init; }

    public string? Role { get; init; }
}

public sealed record AssignRolesRequest(IReadOnlyList<int> RoleIds);
