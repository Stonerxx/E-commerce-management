using System.Data;
using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Shared.Abstractions;
using Oracle.ManagedDataAccess.Client;

namespace ECommerce.Infrastructure.Repositories;

public sealed class AddressRepository : IAddressRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public AddressRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AddressDto>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        var addresses = new List<Address>();

        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        AttachTransaction(command);
        command.CommandText = """
            SELECT id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at
            FROM ADDRESS
            WHERE user_id = :user_id AND is_deleted = 0
            ORDER BY is_default DESC, id DESC
            """;
        command.Parameters.Add(new OracleParameter("user_id", userId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            addresses.Add(MapAddress(reader, userId));
        }

        return addresses.Select(ToDto).ToArray();
    }

    public async Task<AddressDto?> GetByIdAsync(long userId, long addressId, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        AttachTransaction(command);
        command.CommandText = """
            SELECT id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at
            FROM ADDRESS
            WHERE id = :address_id AND user_id = :user_id AND is_deleted = 0
            """;
        command.Parameters.Add(new OracleParameter("address_id", addressId));
        command.Parameters.Add(new OracleParameter("user_id", userId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ToDto(MapAddress(reader, userId));
    }

    public async Task<bool> ExistsAsync(long userId, long addressId, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        AttachTransaction(command);
        command.CommandText = """SELECT COUNT(1) FROM ADDRESS WHERE id = :address_id AND user_id = :user_id AND is_deleted = 0""";
        command.Parameters.Add(new OracleParameter("address_id", addressId));
        command.Parameters.Add(new OracleParameter("user_id", userId));
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<long> CreateAsync(long userId, AddressRequest request, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        if (request.IsDefault)
        {
            await ClearDefaultAsync(connection, userId, cancellationToken);
        }

        var address = ToEntity(userId, request);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        AttachTransaction(command);
        command.CommandText = """
            INSERT INTO ADDRESS(user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default)
            VALUES (:user_id, :receiver_name, :receiver_phone, :province, :city, :district, :detail_address, :is_default)
            RETURNING id INTO :id
            """;
        AddAddressParameters(command, address);
        var idParameter = new OracleParameter("id", OracleDbType.Int64)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(idParameter);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return Convert.ToInt64(idParameter.Value.ToString());
    }

    public async Task UpdateAsync(long userId, long addressId, AddressRequest request, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        if (request.IsDefault)
        {
            await ClearDefaultAsync(connection, userId, cancellationToken);
        }

        var address = ToEntity(userId, request);
        address.Id = addressId;

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        AttachTransaction(command);
        command.CommandText = """
            UPDATE ADDRESS
            SET receiver_name = :receiver_name,
                receiver_phone = :receiver_phone,
                province = :province,
                city = :city,
                district = :district,
                detail_address = :detail_address,
                is_default = :is_default
            WHERE id = :address_id AND user_id = :user_id AND is_deleted = 0
            """;
        AddAddressParameters(command, address);
        command.Parameters.Add(new OracleParameter("address_id", address.Id));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(long userId, long addressId, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        AttachTransaction(command);
        command.CommandText = """
            UPDATE ADDRESS
            SET is_deleted = 1, is_default = 0
            WHERE id = :address_id AND user_id = :user_id AND is_deleted = 0
            """;
        command.Parameters.Add(new OracleParameter("address_id", addressId));
        command.Parameters.Add(new OracleParameter("user_id", userId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SetDefaultAsync(long userId, long addressId, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await ClearDefaultAsync(connection, userId, cancellationToken);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        AttachTransaction(command);
        command.CommandText = """UPDATE ADDRESS SET is_default = 1 WHERE id = :address_id AND user_id = :user_id AND is_deleted = 0""";
        command.Parameters.Add(new OracleParameter("address_id", addressId));
        command.Parameters.Add(new OracleParameter("user_id", userId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ClearDefaultAsync(
        OracleConnection connection,
        long userId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.BindByName = true;
        AttachTransaction(command);
        command.CommandText = """UPDATE ADDRESS SET is_default = 0 WHERE user_id = :user_id AND is_deleted = 0""";
        command.Parameters.Add(new OracleParameter("user_id", userId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddAddressParameters(OracleCommand command, Address address)
    {
        command.Parameters.Add(new OracleParameter("user_id", address.UserId));
        command.Parameters.Add(new OracleParameter("receiver_name", address.ReceiverName));
        command.Parameters.Add(new OracleParameter("receiver_phone", address.ReceiverPhone));
        command.Parameters.Add(new OracleParameter("province", address.Province));
        command.Parameters.Add(new OracleParameter("city", address.City));
        command.Parameters.Add(new OracleParameter("district", address.District));
        command.Parameters.Add(new OracleParameter("detail_address", address.DetailAddress));
        command.Parameters.Add(new OracleParameter("is_default", address.IsDefault));
    }

    private static Address MapAddress(IDataRecord reader, long userId)
    {
        return new Address
        {
            Id = Convert.ToInt64(reader["id"]),
            UserId = userId,
            ReceiverName = Convert.ToString(reader["receiver_name"]) ?? string.Empty,
            ReceiverPhone = Convert.ToString(reader["receiver_phone"]) ?? string.Empty,
            Province = Convert.ToString(reader["province"]) ?? string.Empty,
            City = Convert.ToString(reader["city"]) ?? string.Empty,
            District = Convert.ToString(reader["district"]) ?? string.Empty,
            DetailAddress = Convert.ToString(reader["detail_address"]) ?? string.Empty,
            IsDefault = Convert.ToInt32(reader["is_default"]),
            CreatedAt = Convert.ToDateTime(reader["created_at"])
        };
    }

    private static Address ToEntity(long userId, AddressRequest request)
    {
        return new Address
        {
            UserId = userId,
            ReceiverName = request.ReceiverName.Trim(),
            ReceiverPhone = request.ReceiverPhone.Trim(),
            Province = request.Province.Trim(),
            City = request.City.Trim(),
            District = request.District.Trim(),
            DetailAddress = request.DetailAddress.Trim(),
            IsDefault = request.IsDefault ? 1 : 0
        };
    }

    private static AddressDto ToDto(Address address)
    {
        return new AddressDto(
            address.Id,
            address.ReceiverName,
            address.ReceiverPhone,
            address.Province,
            address.City,
            address.District,
            address.DetailAddress,
            address.IsDefault == 1,
            address.CreatedAt);
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
}
