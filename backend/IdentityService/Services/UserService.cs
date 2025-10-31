using IdentityService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;
using Shared.Models;
using Shared.Services;
using Shared.Utilities;

namespace IdentityService.Services;

public class UserService : IUserService
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<UserService> _logger;
    private readonly IJwtService? _jwtService;

    public UserService(
        IdentityDbContext context,
        ILogger<UserService> logger,
        IJwtService? jwtService = null)
    {
        _context = context;
        _logger = logger;
        _jwtService = jwtService;
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
            _logger.LogWarning("Authentication failed for user: {Email}", dto.Email);
            return null;
        }

        // Generate JWT token if LocalJwtService is available
        if (_jwtService is LocalJwtService localJwtService)
        {
            var token = localJwtService.GenerateToken(user.Id, user.Email, user.Name);
            _logger.LogInformation("User authenticated successfully: {UserId}", user.Id);
            return token;
        }

        // Fallback to simple token (for backwards compatibility)
        _logger.LogWarning("JWT service not available, using fallback token");
        return $"token_{user.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    public async Task<UserSettingsDto?> GetUserSettingsAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return null;
        }

        return new UserSettingsDto
        {
            PreferredUnits = user.PreferredUnits
        };
    }

    public async Task<UserSettingsDto?> UpdateUserSettingsAsync(string userId, UpdateUserSettingsDto dto, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return null;
        }

        // Validate unit system
        if (dto.PreferredUnits != "SI" && dto.PreferredUnits != "Imperial")
        {
            throw new ArgumentException("PreferredUnits must be either 'SI' or 'Imperial'");
        }

        user.PreferredUnits = dto.PreferredUnits;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User settings updated for user: {UserId}", userId);

        return new UserSettingsDto
        {
            PreferredUnits = user.PreferredUnits
        };
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Name = user.Name,
        PreferredUnits = user.PreferredUnits,
        CreatedAt = user.CreatedAt
    };
}





