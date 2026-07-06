using ECommerce.Application.DTOs;

namespace ECommerce.Application.Services;

public interface ICartService
{
    Task<CartDto> GetCartAsync(long userId, CancellationToken cancellationToken = default);

    Task AddItemAsync(long userId, CartItemRequest request, CancellationToken cancellationToken = default);

    Task UpdateItemAsync(long userId, long cartItemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);

    Task RemoveItemAsync(long userId, long cartItemId, CancellationToken cancellationToken = default);

    Task ClearAsync(long userId, CancellationToken cancellationToken = default);
}
