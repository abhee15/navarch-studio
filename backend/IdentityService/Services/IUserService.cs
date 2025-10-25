using Shared.DTOs;

namespace IdentityService.Services;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(string id, CancellationToken cancellationToken);
    Task<UserDto> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken);
    Task<string?> AuthenticateAsync(LoginDto dto, CancellationToken cancellationToken);
    Task<UserSettingsDto?> GetUserSettingsAsync(string userId, CancellationToken cancellationToken);
    Task<UserSettingsDto?> UpdateUserSettingsAsync(string userId, UpdateUserSettingsDto dto, CancellationToken cancellationToken);
}





