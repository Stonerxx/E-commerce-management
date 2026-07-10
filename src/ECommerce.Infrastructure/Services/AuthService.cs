using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Security;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Constants;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserSessionDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var username = NormalizeRequired(request.Username, "用户名不能为空");
        var password = NormalizeRequired(request.Password, "密码不能为空");

        if (password.Length < 6)
        {
            throw new BusinessException(ErrorCodes.ValidationError, "密码长度不能少于6位");
        }

        var existingUser = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (existingUser is not null)
        {
            throw new BusinessException(ErrorCodes.ValidationError, "用户名已存在");
        }

        var user = new User
        {
            Username = username,
            PasswordHash = PasswordHashing.Hash(password),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Status = 1
        };

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var userId = await _userRepository.CreateUserWithDefaultRoleAsync(user, AuthConstants.Roles.User, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            return new UserSessionDto(userId, user.Username, new[] { AuthConstants.Roles.User });
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<UserSessionDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var username = NormalizeRequired(request.Username, "用户名不能为空");
        var password = NormalizeRequired(request.Password, "密码不能为空");

        var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (user is null || !PasswordHashing.Verify(password, user.PasswordHash))
        {
            throw new BusinessException(ErrorCodes.AuthInvalidCredentials, "用户名或密码错误");
        }

        if (user.Status == 0)
        {
            throw new BusinessException(ErrorCodes.UserDisabled, "账号已被禁用");
        }

        await _userRepository.UpdateLastLoginAsync(user.Id, cancellationToken);
        var roles = await _userRepository.GetRoleNamesAsync(user.Id, cancellationToken);
        return new UserSessionDto(user.Id, user.Username, roles);
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private static string NormalizeRequired(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessException(ErrorCodes.ValidationError, message);
        }

        return value.Trim();
    }
}
