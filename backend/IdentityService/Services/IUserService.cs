using Shared.DTOs;

namespace IdentityService.Services;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(string id, CancellationToken cancellationToken);
    Task<UserDto> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken);
    Task<string?> AuthenticateAsync(LoginDto dto, CancellationToken cancellationToken);
}





