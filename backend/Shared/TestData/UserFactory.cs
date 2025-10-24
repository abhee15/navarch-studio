using Shared.Models;
using Shared.DTOs;
using Shared.Utilities;

namespace Shared.TestData;

public static class UserFactory
{
    public static User CreateUser(string? email = null, string? name = null)
    {
        return new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = email ?? $"user{Guid.NewGuid():N}@example.com",
            Name = name ?? $"User {Guid.NewGuid():N}",
            PasswordHash = PasswordHasher.Hash("password123"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = null
        };
    }

    public static CreateUserDto CreateUserDto(string? email = null, string? name = null)
    {
        return new CreateUserDto
        {
            Email = email ?? $"user{Guid.NewGuid():N}@example.com",
            Name = name ?? $"User {Guid.NewGuid():N}",
            Password = "password123"
        };
    }

    public static UserDto CreateUserDtoFromUser(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            CreatedAt = user.CreatedAt
        };
    }
}





