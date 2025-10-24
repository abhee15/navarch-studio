namespace Shared.DTOs;

public record UserDto
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateUserDto
{
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required string Password { get; init; }
}

public record LoginDto
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}





