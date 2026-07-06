namespace ECommerce.Application.DTOs;

public sealed record AddressRequest(
    string ReceiverName,
    string ReceiverPhone,
    string Province,
    string City,
    string District,
    string DetailAddress,
    bool IsDefault);

public sealed record AddressDto(
    long AddressId,
    string ReceiverName,
    string ReceiverPhone,
    string Province,
    string City,
    string District,
    string DetailAddress,
    bool IsDefault,
    DateTime CreatedAt);
