# .NET Core Rules

## General Guidelines

- Use .NET 8 features
- Follow Microsoft's coding conventions
- Use async/await for I/O operations
- Use dependency injection
- Keep controllers thin (delegate to services)
- Use repository pattern for data access

## Project Structure

```
ServiceName/
├── Controllers/          # API endpoints
├── Services/             # Business logic
├── Models/               # Domain models
├── DTOs/                 # Data transfer objects
├── Middleware/           # Custom middleware
├── Configuration/        # Configuration classes
├── Program.cs           # Entry point
└── appsettings.json     # Configuration

ServiceName.Tests/
├── Controllers/
├── Services/
└── Integration/
```

## Controller Pattern

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all users
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(
        CancellationToken cancellationToken)
    {
        try
        {
            var users = await _userService.GetAllUsersAsync(cancellationToken);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Gets user by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(
        string id,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(id, cancellationToken);
        
        if (user == null)
        {
            return NotFound();
        }
        
        return Ok(user);
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> CreateUser(
        [FromBody] CreateUserDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userService.CreateUserAsync(dto, cancellationToken);
        
        return CreatedAtAction(
            nameof(GetUser),
            new { id = user.Id },
            user);
    }
}
```

## Service Pattern

```csharp
namespace IdentityService.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken);
    Task<UserDto?> GetUserByIdAsync(string id, CancellationToken cancellationToken);
    Task<UserDto> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken);
}

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(
        IUserRepository repository,
        ILogger<UserService> logger,
        IPasswordHasher passwordHasher)
    {
        _repository = repository;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync(
        CancellationToken cancellationToken)
    {
        var users = await _repository.GetAllAsync(cancellationToken);
        return users.Select(MapToDto);
    }

    public async Task<UserDto?> GetUserByIdAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        return user != null ? MapToDto(user) : null;
    }

    public async Task<UserDto> CreateUserAsync(
        CreateUserDto dto,
        CancellationToken cancellationToken)
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = dto.Email,
            Name = dto.Name,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(user, cancellationToken);
        
        _logger.LogInformation("User created: {UserId}", user.Id);
        
        return MapToDto(user);
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Name = user.Name,
        CreatedAt = user.CreatedAt
    };
}
```

## Repository Pattern

```csharp
namespace DataService.Repositories;

public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken);
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task AddAsync(T entity, CancellationToken cancellationToken);
    Task UpdateAsync(T entity, CancellationToken cancellationToken);
    Task DeleteAsync(string id, CancellationToken cancellationToken);
}

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<User>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

## DTOs and Validation

```csharp
using System.ComponentModel.DataAnnotations;

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
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public required string Name { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string Password { get; init; }
}
```

## Entity Framework DbContext

```csharp
using Microsoft.EntityFrameworkCore;

namespace DataService.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
        });
    }
}
```

## Program.cs Configuration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var configuration = builder.Configuration;

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Identity Service API",
        Version = "v1"
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(configuration.GetConnectionString("DefaultConnection")!);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(configuration["FrontendUrl"]!)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

## Testing with xUnit

```csharp
using Xunit;
using Moq;
using FluentAssertions;

namespace IdentityService.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        
        _sut = new UserService(
            _repositoryMock.Object,
            _loggerMock.Object,
            _passwordHasherMock.Object);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = "1", Email = "test@example.com", Name = "Test" }
        };
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _sut.GetAllUsersAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task CreateUserAsync_CreatesUser()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Email = "new@example.com",
            Name = "New User",
            Password = "password123"
        };
        _passwordHasherMock
            .Setup(p => p.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        // Act
        var result = await _sut.CreateUserAsync(dto, CancellationToken.None);

        // Assert
        result.Email.Should().Be(dto.Email);
        _repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

## Middleware

```csharp
namespace ApiGateway.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Items[CorrelationIdHeader] = correlationId;
        context.Response.Headers.Add(CorrelationIdHeader, correlationId);

        await _next(context);
    }
}
```

## Best Practices

- ✅ Use `async`/`await` for I/O operations
- ✅ Pass `CancellationToken` to all async methods
- ✅ Use dependency injection
- ✅ Use records for DTOs (immutability)
- ✅ Use nullable reference types
- ✅ Log important operations
- ✅ Use health checks
- ✅ Enable Swagger in development
- ✅ Use CORS properly
- ✅ Validate input with data annotations

## Avoid

- ❌ Blocking calls (.Result, .Wait())
- ❌ Business logic in controllers
- ❌ Direct database access in controllers
- ❌ Exposing entity models as DTOs
- ❌ Ignoring cancellation tokens
- ❌ Not handling exceptions
- ❌ Using `DateTime.Now` (use `DateTime.UtcNow`)

## Debugging .NET Applications in Cloud

### Core Principle
**Check infrastructure and configuration before debugging application code.**

### When Application Fails in Cloud But Works Locally

Follow this order:

1. **Check Infrastructure** (Is logging enabled?)
2. **Check Configuration** (Are env vars set?)
3. **Check Application** (Is there a code bug?)

### Common Cloud Scenarios

#### 1. No Logs in CloudWatch

**Symptom**: Application logs not appearing in CloudWatch

**Debug Steps**:
```bash
# 1. Verify observability is configured in Terraform
cat terraform/deploy/modules/app-runner/main.tf | grep -A 3 "observability_configuration"

# 2. If missing, this is an infrastructure issue, not application
# Fix: Add observability configuration to Terraform

# 3. Only if observability exists, check application logging
# Verify Serilog is configured in Program.cs
```

**Do NOT**:
- ❌ Try different log levels
- ❌ Add more logging statements
- ❌ Modify logging configuration
- ✅ First verify infrastructure supports logging

#### 2. Database Connection Fails

**Symptom**: `Npgsql.NpgsqlException: Failed to connect` or timeout

**Debug Steps**:
```bash
# Layer 1: Infrastructure
# 1. Verify RDS exists and is available
aws rds describe-db-instances --region us-east-1

# 2. Check security groups allow App Runner → RDS traffic
cat terraform/deploy/modules/rds/main.tf | grep security_group

# 3. Verify VPC connector is configured
cat terraform/deploy/modules/app-runner/main.tf | grep vpc_connector

# Layer 2: Configuration
# 4. Check connection string format
# Should be: Host=<endpoint>;Port=5432;Database=<db>;Username=<user>;Password=<pass>
aws apprunner describe-service --service-arn <arn> | grep ConnectionStrings

# 5. Verify password is accessible from Secrets Manager
aws secretsmanager get-secret-value --secret-id <id>

# Layer 3: Application
# 6. Only if all above pass, check EF Core configuration
# Review Program.cs DbContext setup
```

**Connection Timeout Decision Tree**:
- Timeout < 5 seconds → Infrastructure (security groups/VPC)
- "password authentication failed" → Configuration (wrong credentials)
- "database does not exist" → Configuration (wrong database name)
- "relation does not exist" → Application (migrations not run)

#### 3. Migrations Not Running

**Symptom**: "relation does not exist" errors

**Debug Steps**:
```csharp
// 1. Check ASPNETCORE_ENVIRONMENT in Terraform
// Must be "Staging" or "Production" for auto-migrations
cat terraform/deploy/modules/app-runner/main.tf | grep ASPNETCORE_ENVIRONMENT

// 2. Verify auto-migration code in Program.cs
if (app.Environment.EnvironmentName != "Development")
{
    Log.Information("Auto-applying migrations in {Environment}...", 
        app.Environment.EnvironmentName);
    await dbContext.Database.MigrateAsync();
    Log.Information("✅ Migrations applied successfully!");
}

// 3. Check CloudWatch logs for migration status
aws logs tail /aws/apprunner/<service>/service --since 10m | grep -i migration
```

**Common Issues**:
- `ASPNETCORE_ENVIRONMENT = "Dev"` → Won't run migrations (use "Staging")
- Missing `await dbContext.Database.MigrateAsync()` → Migrations don't run
- Using `builder.Environment` instead of `app.Environment` → Wrong environment check

#### 4. Environment Variables Not Set

**Symptom**: `NullReferenceException` or configuration errors

**Debug Steps**:
```bash
# 1. Check Terraform configuration
cat terraform/deploy/modules/app-runner/main.tf | grep -A 20 "runtime_environment_variables"

# 2. Verify in AWS
aws apprunner describe-service --service-arn <arn> --query "Service.SourceConfiguration.ImageRepository.ImageConfiguration.RuntimeEnvironmentVariables"

# 3. Only if set correctly, check application code
# Review appsettings.json and Program.cs configuration binding
```

#### 5. Service Returns 500 Error

**Symptom**: API returns 500 Internal Server Error

**Debug Steps**:
```bash
# 1. Check if logs are available (infrastructure)
aws logs tail /aws/apprunner/<service>/service --since 5m

# 2. If no logs, fix observability first (infrastructure issue)

# 3. If logs exist, look for exceptions
aws logs tail /aws/apprunner/<service>/service --since 5m | grep -i exception

# 4. Identify exception type:
# - NullReferenceException → Configuration (missing env var)
# - NpgsqlException → Infrastructure (database connectivity)
# - Custom exception → Application (code bug)
```

### Health Check Debugging

**Symptom**: App Runner service keeps restarting

**Debug Steps**:
```bash
# 1. Check health endpoint configuration in Terraform
cat terraform/deploy/modules/app-runner/main.tf | grep -A 6 "health_check_configuration"

# 2. Verify health endpoint exists in application
# Should have: app.MapHealthChecks("/health");

# 3. Check health check timeout
# If migrations run on startup, timeout must be sufficient (30+ seconds)

# 4. Test health endpoint locally
curl http://localhost:8080/health
```

### Structured Logging Best Practices

```csharp
// ✅ Good: Structured logging with context
Log.Information("Processing order {OrderId} for user {UserId}", orderId, userId);

// ❌ Bad: String concatenation
Log.Information($"Processing order {orderId} for user {userId}");

// ✅ Good: Log important lifecycle events
Log.Information("🔄 Starting database migration check...");
Log.Information("✅ Database schema is up to date");
Log.Error(ex, "❌ Migration check failed: {Message}", ex.Message);

// ✅ Good: Include environment context
Log.Information("Running in {Environment} environment", app.Environment.EnvironmentName);
```

### Configuration Validation

Always validate critical configuration on startup:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Validate required configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("ConnectionString 'DefaultConnection' not configured");
}

var cognitoUserPoolId = builder.Configuration["Cognito:UserPoolId"];
if (string.IsNullOrEmpty(cognitoUserPoolId))
{
    throw new InvalidOperationException("Cognito:UserPoolId not configured");
}

Log.Information("✅ Configuration validated successfully");
```

### Debugging Checklist

Before debugging .NET code in cloud:

- [ ] Verify CloudWatch logs are available
- [ ] Check `ASPNETCORE_ENVIRONMENT` is set correctly
- [ ] Verify connection strings are formatted correctly
- [ ] Confirm database exists and is accessible
- [ ] Check if migrations have run
- [ ] Validate all required environment variables are set
- [ ] Review CloudWatch logs for exceptions
- [ ] Only then debug application code

### Common Mistakes

❌ **Assuming configuration is correct** - Always verify env vars in Terraform
❌ **Not checking logs first** - Logs tell you exactly what's wrong
❌ **Debugging locally** - Local and cloud have different configurations
❌ **Ignoring environment name** - Dev/Staging/Production behave differently
❌ **Not using structured logging** - Makes cloud debugging much harder

### Best Practice Workflow

1. **Issue in Cloud** → Check CloudWatch logs first
2. **No Logs** → Infrastructure issue (observability not configured)
3. **Exception in Logs** → Identify exception type
4. **Connection Error** → Check infrastructure (security groups, VPC)
5. **Configuration Error** → Check Terraform env vars
6. **Application Error** → Debug code (only after 1-5 verified)

### Cross-References

- [Full Debugging Methodology](./debugging-methodology.md)
- [Troubleshooting Flowchart](./troubleshooting-flowchart.md)
- [Terraform Debugging](./terraform.md#debugging-terraform-managed-infrastructure)






