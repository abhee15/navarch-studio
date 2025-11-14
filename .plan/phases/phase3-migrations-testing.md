# Phase 3: Database Migrations & Testing

## Goal

Implement database schema management with EF Core migrations including schema separation, add soft delete support, and establish comprehensive testing strategy. This phase ensures database changes are versioned, repeatable, and testable using Entity Framework Core migrations. (Flyway will be introduced in Phase 6 for production deployments.)

## Prerequisites

- Phase 2 completed (microservices working)
- Understanding of EF Core migrations
- Docker and docker-compose working
- .NET 9 SDK installed

## Deliverables Checklist

### 1. Schema Separation Setup

**Why Schema Separation?**
Both IdentityService and DataService share the same PostgreSQL database instance but need separate migration histories to avoid conflicts. Using separate schemas (`identity` and `data`) prevents EF Core migration tables from conflicting.

#### Update Models with Soft Delete Support

**File**: `backend/Shared/Models/User.cs`

```csharp
namespace Shared.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

**File**: `backend/Shared/Models/Product.cs`

```csharp
namespace Shared.Models;

public class Product
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

#### Update IdentityDbContext with Schema

**File**: `backend/IdentityService/Data/IdentityDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace IdentityService.Data;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use 'identity' schema to separate from other services
        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);

            // Soft delete support
            entity.HasQueryFilter(e => e.DeletedAt == null);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is User && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var user = (User)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                user.CreatedAt = DateTime.UtcNow;
            }

            user.UpdatedAt = DateTime.UtcNow;
        }
    }
}
```

#### Update DataDbContext with Schema

**File**: `backend/DataService/Data/DataDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace DataService.Data;

public class DataDbContext : DbContext
{
    public DataDbContext(DbContextOptions<DataDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use 'data' schema to separate from other services
        modelBuilder.HasDefaultSchema("data");

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");

            // Soft delete support
            entity.HasQueryFilter(e => e.DeletedAt == null);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Product && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var product = (Product)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                product.CreatedAt = DateTime.UtcNow;
            }

            product.UpdatedAt = DateTime.UtcNow;
        }
    }
}
```

### 2. Generate and Apply EF Core Migrations

#### Create Migrations

```bash
# Identity Service migrations
cd backend/IdentityService
dotnet ef migrations add AddSchemaAndSoftDelete

# Data Service migrations
cd ../DataService
dotnet ef migrations add AddSchemaAndSoftDelete
```

#### Apply Migrations Locally

```bash
# Identity Service
cd backend/IdentityService
dotnet ef database update

# Data Service
cd ../DataService
dotnet ef database update
```

#### Verify Schema Creation

Connect to PostgreSQL and verify schemas:

```bash
docker compose exec postgres psql -U postgres -d sri_template_dev

# List all schemas
\dn

# You should see:
#   identity
#   data
#   public

# List tables in identity schema
\dt identity.*

# List tables in data schema
\dt data.*
```

### 3. Database Seed Data

#### Development Seed Data

**File**: `database/seeds/dev-seed.sql`

```sql
-- Seed development data
-- Run with: docker compose exec -T postgres psql -U postgres -d sri_template_dev -f /path/to/dev-seed.sql

-- Seed users (password is 'password' hashed with BCrypt)
INSERT INTO identity."Users" ("Id", "Email", "Name", "PasswordHash", "CreatedAt", "UpdatedAt") VALUES
('550e8400-e29b-41d4-a716-446655440001', 'admin@example.com', 'Admin User', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440002', 'user@example.com', 'Test User', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', NOW(), NOW())
ON CONFLICT ("Email") DO NOTHING;

-- Seed products
INSERT INTO data."Products" ("Id", "Name", "Price", "Description", "CreatedAt", "UpdatedAt") VALUES
('660e8400-e29b-41d4-a716-446655440001', 'Laptop', 999.99, 'High-performance laptop', NOW(), NOW()),
('660e8400-e29b-41d4-a716-446655440002', 'Mouse', 29.99, 'Wireless mouse', NOW(), NOW()),
('660e8400-e29b-41d4-a716-446655440003', 'Keyboard', 79.99, 'Mechanical keyboard', NOW(), NOW()),
('660e8400-e29b-41d4-a716-446655440004', 'Monitor', 299.99, '27-inch 4K monitor', NOW(), NOW())
ON CONFLICT ("Id") DO NOTHING;
```

**Important Note**: PostgreSQL is case-sensitive with quoted identifiers. EF Core creates tables and columns with PascalCase (e.g., `"Users"`, `"Products"`, `"Id"`, `"Email"`), so the seed SQL must use quoted identifiers with the exact casing.

#### Running Seed Data

```bash
# Method 1: Copy file into container and run
docker compose cp database/seeds/dev-seed.sql postgres:/tmp/dev-seed.sql
docker compose exec postgres psql -U postgres -d sri_template_dev -f /tmp/dev-seed.sql

# Method 2: Pipe directly from host
docker compose exec -T postgres psql -U postgres -d sri_template_dev < database/seeds/dev-seed.sql
```

#### Verifying Seed Data

To verify the seeded data was inserted correctly, you can query the tables. Note that PowerShell has issues with escaped quotes, so it's easier to use a SQL file or enter psql interactively:

```bash
# Enter psql interactively
docker compose exec postgres psql -U postgres -d sri_template_dev

# Then run these queries:
# Check user count
SELECT COUNT(*) FROM identity."Users";

# Check product count
SELECT COUNT(*) FROM data."Products";

# View users
SELECT "Email", "Name" FROM identity."Users";

# View products
SELECT "Name", "Price" FROM data."Products";

# Exit psql
\q
```

**Login Credentials for Testing:**

- Email: `admin@example.com` or `user@example.com`
- Password: `password`

#### Common Database Operations

**Reset Database**:

```bash
# Stop services and remove volumes
docker compose down -v

# Start database
docker compose up -d postgres

# Wait for database to be ready (10 seconds)
sleep 10

# Apply migrations
cd backend/IdentityService
dotnet ef database update

cd ../DataService
dotnet ef database update

# Seed data
docker compose exec -T postgres psql -U postgres -d sri_template_dev < database/seeds/dev-seed.sql
```

**Check Schema and Tables**:

```bash
# Connect to PostgreSQL
docker compose exec postgres psql -U postgres -d sri_template_dev

# List schemas
\dn

# List tables in identity schema
\dt identity.*

# List tables in data schema
\dt data.*

# View migration history
SELECT * FROM identity."__EFMigrationsHistory";
SELECT * FROM data."__EFMigrationsHistory";

# Exit
\q
```

### 4. Test Infrastructure Setup

#### Create Test Project Structure

```bash
# Create test projects if they don't exist
cd backend

# IdentityService tests
dotnet new xunit -n IdentityService.Tests --framework net9.0
dotnet sln add IdentityService.Tests
cd IdentityService.Tests
dotnet add reference ../IdentityService
dotnet add reference ../Shared
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Moq
dotnet add package FluentAssertions
cd ..

# DataService tests
dotnet new xunit -n DataService.Tests --framework net9.0
dotnet sln add DataService.Tests
cd DataService.Tests
dotnet add reference ../DataService
dotnet add reference ../Shared
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Moq
dotnet add package FluentAssertions
cd ..

# Create test helper directories in Shared
mkdir -p Shared/TestData
mkdir -p Shared/TestBase
```

### 5. Test Data Factories

**File**: `backend/Shared/TestData/UserFactory.cs`

```csharp
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
```

**File**: `backend/Shared/TestData/ProductFactory.cs`

```csharp
using Shared.Models;
using Shared.DTOs;

namespace Shared.TestData;

public static class ProductFactory
{
    public static Product CreateProduct(string? name = null, decimal? price = null)
    {
        return new Product
        {
            Id = Guid.NewGuid().ToString(),
            Name = name ?? $"Product {Guid.NewGuid():N}",
            Price = price ?? 99.99m,
            Description = $"Description for {name ?? "Product"}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = null
        };
    }

    public static CreateProductDto CreateProductDto(string? name = null, decimal? price = null)
    {
        return new CreateProductDto
        {
            Name = name ?? $"Product {Guid.NewGuid():N}",
            Price = price ?? 99.99m,
            Description = $"Description for {name ?? "Product"}"
        };
    }

    public static ProductDto CreateProductDtoFromProduct(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Description = product.Description,
            CreatedAt = product.CreatedAt
        };
    }
}
```

### 6. Integration Tests

**File**: `backend/IdentityService.Tests/Integration/UsersControllerIntegrationTests.cs`

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using IdentityService.Data;
using Shared.DTOs;
using Xunit;
using FluentAssertions;

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
                // Remove the real database
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
                if (descriptor != null)
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

    [Fact]
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
        response.Should().BeSuccessful();
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(createUserDto.Email);
        user.Name.Should().Be(createUserDto.Name);
    }

    [Fact]
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
        response.Should().BeSuccessful();
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Id.Should().Be(createdUser.Id);
    }
}
```

**File**: `backend/DataService.Tests/Integration/ProductsControllerIntegrationTests.cs`

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using DataService.Data;
using Shared.DTOs;
using Xunit;
using FluentAssertions;

namespace DataService.Tests.Integration;

public class ProductsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProductsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real database
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<DataDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database
                services.AddDbContext<DataDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsEmptyList_Initially()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/products");

        // Assert
        response.Should().BeSuccessful();
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        products.Should().NotBeNull();
        products.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreatedProduct()
    {
        // Arrange
        var createProductDto = new CreateProductDto
        {
            Name = "Test Product",
            Price = 99.99m,
            Description = "Test Description"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/products", createProductDto);

        // Assert
        response.Should().BeSuccessful();
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.Name.Should().Be(createProductDto.Name);
        product.Price.Should().Be(createProductDto.Price);
    }
}
```

### 7. Unit Tests for Services

**File**: `backend/IdentityService.Tests/Services/UserServiceTests.cs`

```csharp
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
```

### 8. Running Tests

**Backend Tests**:

```bash
# Run all backend tests
cd backend
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run tests for specific project
cd IdentityService.Tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=lcov
```

**Frontend Tests**:

```bash
# Run all frontend tests
cd frontend
npm test

# Run with coverage
npm test -- --coverage

# Run in watch mode
npm test -- --watch
```

**Run All Tests**:

```bash
# Backend
cd backend && dotnet test && cd ..

# Frontend
cd frontend && npm test && cd ..
```

## Validation

After completing this phase, verify:

- [ ] Schema separation configured (`identity` and `data` schemas)
- [ ] Models updated with `UpdatedAt` and `DeletedAt` properties
- [ ] EF Core migrations generated for both services
- [ ] Migrations applied successfully (check with `\dn` and `\dt` in psql)
- [ ] Soft delete query filters working
- [ ] Timestamp auto-update working
- [ ] Test projects created and configured
- [ ] Test data factories implemented
- [ ] Integration tests pass
- [ ] Unit tests pass
- [ ] Seed data SQL file created
- [ ] Can manually seed database successfully
- [ ] All tests run with `dotnet test`

## Next Steps

Proceed to [Phase 4: AWS Infrastructure Setup](phase4-aws-setup.md)

## Notes

- **Migration Strategy**: Using EF Core migrations for development (Phase 3). Flyway will be introduced in Phase 6 for production deployments.
- **Schema Separation**: Critical for preventing migration conflicts when multiple services share the same database instance.
- **Soft Delete**: Implemented using `DeletedAt` column and EF Core query filters. Deleted records remain in database but are filtered from queries.
- **Test Strategy**:
  - Unit tests for services (with in-memory database)
  - Integration tests for controllers (with in-memory database)
  - Test factories for creating test data
- **Database Instance**: Both IdentityService and DataService share the same PostgreSQL instance (`sri_template_dev`) but use separate schemas.
- **Migration History**: Each service maintains its own `__EFMigrationsHistory` table in its respective schema (`identity.__EFMigrationsHistory` and `data.__EFMigrationsHistory`).
- **Seed Data**: Simple SQL file approach for cross-platform compatibility. Automation scripts will be added in Phase 8.
- **Commands**: All database operations documented as inline commands for maximum flexibility and cross-platform support.

## Architecture Diagram

```
PostgreSQL Instance (sri_template_dev)
├── Schema: identity
│   ├── Table: users
│   └── Table: __EFMigrationsHistory
├── Schema: data
│   ├── Table: products
│   └── Table: __EFMigrationsHistory
└── Schema: public (default, unused)
```





