using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public sealed class AddressService : IAddressService
{
    private readonly IAddressRepository _addressRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddressService(IAddressRepository addressRepository, IUnitOfWork unitOfWork)
    {
        _addressRepository = addressRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<AddressDto>> GetMyAddressesAsync(long userId, CancellationToken cancellationToken = default)
    {
        EnsureUserId(userId);
        return _addressRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<AddressDto> GetForOrderAsync(long userId, long addressId, CancellationToken cancellationToken = default)
    {
        EnsureUserId(userId);
        EnsureAddressId(addressId);

        var address = await _addressRepository.GetByIdAsync(userId, addressId, cancellationToken);
        if (address is null)
        {
            throw new BusinessException(ErrorCodes.ResourceNotFound, "收货地址不存在或不属于当前用户");
        }

        return address;
    }

    public async Task<long> CreateAsync(long userId, AddressRequest request, CancellationToken cancellationToken = default)
    {
        EnsureUserId(userId);
        ValidateAddress(request);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var addressId = await _addressRepository.CreateAsync(userId, request, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            return addressId;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateAsync(long userId, long addressId, AddressRequest request, CancellationToken cancellationToken = default)
    {
        EnsureUserId(userId);
        EnsureAddressId(addressId);
        ValidateAddress(request);
        await EnsureAddressExistsAsync(userId, addressId, cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _addressRepository.UpdateAsync(userId, addressId, request, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteAsync(long userId, long addressId, CancellationToken cancellationToken = default)
    {
        EnsureUserId(userId);
        EnsureAddressId(addressId);
        await EnsureAddressExistsAsync(userId, addressId, cancellationToken);
        await _addressRepository.DeleteAsync(userId, addressId, cancellationToken);
    }

    public async Task SetDefaultAsync(long userId, long addressId, CancellationToken cancellationToken = default)
    {
        EnsureUserId(userId);
        EnsureAddressId(addressId);
        await EnsureAddressExistsAsync(userId, addressId, cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _addressRepository.SetDefaultAsync(userId, addressId, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task EnsureAddressExistsAsync(long userId, long addressId, CancellationToken cancellationToken)
    {
        if (!await _addressRepository.ExistsAsync(userId, addressId, cancellationToken))
        {
            throw new BusinessException(ErrorCodes.ResourceNotFound, "收货地址不存在");
        }
    }

    private static void EnsureUserId(long userId)
    {
        if (userId <= 0)
        {
            throw new BusinessException(ErrorCodes.AuthForbidden, "无法识别当前用户");
        }
    }

    private static void EnsureAddressId(long addressId)
    {
        if (addressId <= 0)
        {
            throw new BusinessException(ErrorCodes.ValidationError, "地址ID必须大于0");
        }
    }

    private static void ValidateAddress(AddressRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReceiverName)
            || string.IsNullOrWhiteSpace(request.ReceiverPhone)
            || string.IsNullOrWhiteSpace(request.Province)
            || string.IsNullOrWhiteSpace(request.City)
            || string.IsNullOrWhiteSpace(request.District)
            || string.IsNullOrWhiteSpace(request.DetailAddress))
        {
            throw new BusinessException(ErrorCodes.ValidationError, "收货地址字段不能为空");
        }
    }
}
