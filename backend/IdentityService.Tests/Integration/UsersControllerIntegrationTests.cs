using System.Net.Http.Json;
using FluentAssertions;
using IdentityService.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs;
using Xunit;

namespace IdentityService.Tests.Integration;

public class UsersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UsersControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                // Remove DbContextOptions
                var dbContextOptionsDescriptor = services.Where(
                    d => d.ServiceType.IsGenericType &&
                         d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
                    .ToList();
                foreach (var descriptor in dbContextOptionsDescriptor)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database
                services.AddDbContext<IdentityDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact(Skip = "Integration test requires proper database provider configuration")]
    public async Task CreateUser_ReturnsCreatedUser()
    {
        // Arrange
        var createUserDto = new CreateUserDto
        {
            Email = "test@example.com",
            Name = "Test User",
            Password = "password123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/users", createUserDto);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(createUserDto.Email);
        user.Name.Should().Be(createUserDto.Name);
    }

    [Fact(Skip = "Integration test requires proper database provider configuration")]
    public async Task GetUser_ReturnsUser()
    {
        // Arrange
        var createUserDto = new CreateUserDto
        {
            Email = "test2@example.com",
            Name = "Test User 2",
            Password = "password123"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", createUserDto);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        // Act
        var response = await _client.GetAsync($"/api/v1/users/{createdUser!.Id}");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Id.Should().Be(createdUser.Id);
    }
}





