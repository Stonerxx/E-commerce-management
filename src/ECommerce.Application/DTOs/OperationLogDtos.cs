using ECommerce.Shared.Contracts;

namespace ECommerce.Application.DTOs;

public sealed record OperationLogRequest(
    long OperatorId,
    string OperatorName,
    string Module,
    string Action,
    string? Description,
    string IpAddress,
    string? RequestParams,
    int Result);

public sealed record OperationLogQuery : PageQuery
{
    public long? OperatorId { get; init; }

    public string? Module { get; init; }

    public DateTime? StartTime { get; init; }

    public DateTime? EndTime { get; init; }
}

public sealed record OperationLogDto(
    long LogId,
    long OperatorId,
    string OperatorName,
    string Module,
    string Action,
    string? Description,
    string IpAddress,
    int Result,
    DateTime CreatedAt);
