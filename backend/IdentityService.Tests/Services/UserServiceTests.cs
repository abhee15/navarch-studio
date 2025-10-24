using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using IdentityService.Data;
using IdentityService.Services;
using Shared.DTOs;
using Shared.TestData;
using Xunit;
using FluentAssertions;

namespace IdentityService.Tests.Services;

public class UserServiceTests
{
    private readonly IdentityDbContext _context;
    private readonly UserService _service;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new IdentityDbContext(options);
        _service = new UserService(_context, Mock.Of<ILogger<UserService>>());
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsUser_WhenExists()
    {
        // Arrange
        var user = UserFactory.CreateUser();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserByIdAsync(user.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = await _service.GetUserByIdAsync("nonexistent-id", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateUserAsync_CreatesUser_WithHashedPassword()
    {
        // Arrange
        var createDto = UserFactory.CreateUserDto();

        // Act
        var result = await _service.CreateUserAsync(createDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(createDto.Email);
        result.Name.Should().Be(createDto.Name);

        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == result.Id);
        savedUser.Should().NotBeNull();
        savedUser!.PasswordHash.Should().NotBe(createDto.Password); // Password should be hashed
    }
}





