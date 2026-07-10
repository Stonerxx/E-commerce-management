using ECommerce.Application.DTOs;

namespace ECommerce.Application.Services;

public interface IAuthService
{
    Task<UserSessionDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<UserSessionDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);
}
