namespace ECommerce.Application.DTOs;

public sealed record StatusUpdateRequest(int Status);

public sealed record CancelOrderRequest(string? Reason);
