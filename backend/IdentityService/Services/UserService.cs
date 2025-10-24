using Microsoft.EntityFrameworkCore;
using Shared.DTOs;
using Shared.Models;
using Shared.Utilities;
using IdentityService.Data;

namespace IdentityService.Services;

public class UserService : IUserService
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(IdentityDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserDto?> GetUserByIdAsync(string id, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return user != null ? MapToDto(user) : null;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = dto.Email,
            Name = dto.Name,
            PasswordHash = PasswordHasher.Hash(dto.Password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User created: {UserId}", user.Id);
        return MapToDto(user);
    }

    public async Task<string?> AuthenticateAsync(LoginDto dto, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email, cancellationToken);

        if (user == null || !PasswordHasher.Verify(dto.Password, user.PasswordHash))
        {
            return null;
        }

        // For now, return a simple token (will be replaced with JWT in Phase 6)
        return $"token_{user.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Name = user.Name,
        CreatedAt = user.CreatedAt
    };
}





