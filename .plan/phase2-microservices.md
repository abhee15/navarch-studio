# Phase 2: Microservices Split

## Goal

Refactor the monolith backend into 3 microservices: Identity Service, API Gateway, and Data Service. Each service will have its own responsibility and communicate via HTTP.

## Prerequisites

- Phase 1 completed (working monolith)
- Understanding of microservices architecture
- Docker and docker-compose working

## Deliverables Checklist

### 1. Create Shared Library

#### Create Shared Project

```bash
cd backend
dotnet new classlib -n Shared
dotnet sln add Shared
```

#### Create DTOs

**File**: `backend/Shared/DTOs/UserDto.cs`

```csharp
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
```

**File**: `backend/Shared/DTOs/ProductDto.cs`

```csharp
namespace Shared.DTOs;

public record ProductDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public required string Description { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateProductDto
{
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public required string Description { get; init; }
}
```

#### Create Common Models

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
}
```

#### Create Utilities

**File**: `backend/Shared/Utilities/PasswordHasher.cs`

```csharp
using BCrypt.Net;

namespace Shared.Utilities;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public static bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

### 2. Identity Service (Port 5001)

#### Create Project

```bash
cd backend
dotnet new webapi -n IdentityService
dotnet sln add IdentityService
cd IdentityService
dotnet add reference ../Shared
dotnet add package BCrypt.Net-Next
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt
```

#### Create Services

**File**: `backend/IdentityService/Services/IUserService.cs`

```csharp
using Shared.DTOs;

namespace IdentityService.Services;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(string id, CancellationToken cancellationToken);
    Task<UserDto> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken);
    Task<string?> AuthenticateAsync(LoginDto dto, CancellationToken cancellationToken);
}
```

**File**: `backend/IdentityService/Services/UserService.cs`

```csharp
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
```

#### Create DbContext

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

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });
    }
}
```

#### Create Controllers

**File**: `backend/IdentityService/Controllers/UsersController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;
using IdentityService.Services;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(string id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(id, cancellationToken);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
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

**File**: `backend/IdentityService/Controllers/AuthController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;
using IdentityService.Services;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService userService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login(
        [FromBody] LoginDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var token = await _userService.AuthenticateAsync(dto, cancellationToken);

        if (token == null)
        {
            return Unauthorized("Invalid credentials");
        }

        return Ok(new { token });
    }
}
```

#### Configure Program.cs

```csharp
using Microsoft.EntityFrameworkCore;
using IdentityService.Data;
using IdentityService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IUserService, UserService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

### 3. Data Service (Port 5003)

#### Create Project

```bash
cd backend
dotnet new webapi -n DataService
dotnet sln add DataService
cd DataService
dotnet add reference ../Shared
```

#### Create Services

**File**: `backend/DataService/Services/IProductService.cs`

```csharp
using Shared.DTOs;

namespace DataService.Services;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync(CancellationToken cancellationToken);
    Task<ProductDto?> GetProductByIdAsync(string id, CancellationToken cancellationToken);
    Task<ProductDto> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken);
}
```

**File**: `backend/DataService/Services/ProductService.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;
using Shared.Models;
using DataService.Data;

namespace DataService.Services;

public class ProductService : IProductService
{
    private readonly DataDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(DataDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync(CancellationToken cancellationToken)
    {
        var products = await _context.Products
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return products.Select(MapToDto);
    }

    public async Task<ProductDto?> GetProductByIdAsync(string id, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return product != null ? MapToDto(product) : null;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Price = dto.Price,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product created: {ProductId}", product.Id);
        return MapToDto(product);
    }

    private static ProductDto MapToDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price,
        Description = product.Description,
        CreatedAt = product.CreatedAt
    };
}
```

#### Create DbContext

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

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
        });
    }
}
```

#### Create Controllers

**File**: `backend/DataService/Controllers/ProductsController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;
using DataService.Services;

namespace DataService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllProductsAsync(cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(string id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductByIdAsync(id, cancellationToken);

        if (product == null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        [FromBody] CreateProductDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var product = await _productService.CreateProductAsync(dto, cancellationToken);

        return CreatedAtAction(
            nameof(GetProduct),
            new { id = product.Id },
            product);
    }
}
```

### 4. API Gateway (Port 5002)

#### Create Project

```bash
cd backend
dotnet new webapi -n ApiGateway
dotnet sln add ApiGateway
cd ApiGateway
dotnet add package Microsoft.AspNetCore.HttpOverrides
dotnet add package Polly
```

#### Create Services

**File**: `backend/ApiGateway/Services/IHttpClientService.cs`

```csharp
namespace ApiGateway.Services;

public interface IHttpClientService
{
    Task<HttpResponseMessage> GetAsync(string service, string endpoint, CancellationToken cancellationToken);
    Task<HttpResponseMessage> PostAsync(string service, string endpoint, HttpContent content, CancellationToken cancellationToken);
}
```

**File**: `backend/ApiGateway/Services/HttpClientService.cs`

```csharp
using System.Text;
using System.Text.Json;

namespace ApiGateway.Services;

public class HttpClientService : IHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpClientService> _logger;
    private readonly IConfiguration _configuration;

    public HttpClientService(
        HttpClient httpClient,
        ILogger<HttpClientService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<HttpResponseMessage> GetAsync(string service, string endpoint, CancellationToken cancellationToken)
    {
        var baseUrl = GetServiceBaseUrl(service);
        var url = $"{baseUrl}/{endpoint.TrimStart('/')}";

        _logger.LogInformation("Forwarding GET request to {Url}", url);
        return await _httpClient.GetAsync(url, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsync(string service, string endpoint, HttpContent content, CancellationToken cancellationToken)
    {
        var baseUrl = GetServiceBaseUrl(service);
        var url = $"{baseUrl}/{endpoint.TrimStart('/')}";

        _logger.LogInformation("Forwarding POST request to {Url}", url);
        return await _httpClient.PostAsync(url, content, cancellationToken);
    }

    private string GetServiceBaseUrl(string service)
    {
        return service.ToLower() switch
        {
            "identity" => _configuration["Services:IdentityService"] ?? "http://localhost:5001",
            "data" => _configuration["Services:DataService"] ?? "http://localhost:5003",
            _ => throw new ArgumentException($"Unknown service: {service}")
        };
    }
}
```

#### Create Controllers

**File**: `backend/ApiGateway/Controllers/UsersController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using ApiGateway.Services;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IHttpClientService httpClientService, ILogger<UsersController> logger)
    {
        _httpClientService = httpClientService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id, CancellationToken cancellationToken)
    {
        var response = await _httpClientService.GetAsync("identity", $"api/v1/users/{id}", cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return StatusCode((int)response.StatusCode, content);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] object dto,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClientService.PostAsync("identity", "api/v1/users", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return StatusCode((int)response.StatusCode, responseContent);
    }
}
```

**File**: `backend/ApiGateway/Controllers/ProductsController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using ApiGateway.Services;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IHttpClientService httpClientService, ILogger<ProductsController> logger)
    {
        _httpClientService = httpClientService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        var response = await _httpClientService.GetAsync("data", "api/v1/products", cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return StatusCode((int)response.StatusCode, content);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(string id, CancellationToken cancellationToken)
    {
        var response = await _httpClientService.GetAsync("data", $"api/v1/products/{id}", cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return StatusCode((int)response.StatusCode, content);
    }
}
```

**File**: `backend/ApiGateway/Controllers/AuthController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using ApiGateway.Services;
using System.Text;
using System.Text.Json;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IHttpClientService httpClientService, ILogger<AuthController> logger)
    {
        _httpClientService = httpClientService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] object dto,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClientService.PostAsync("identity", "api/v1/auth/login", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return StatusCode((int)response.StatusCode, responseContent);
    }
}
```

#### Configure Program.cs

```csharp
using ApiGateway.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// HTTP Client
builder.Services.AddHttpClient<IHttpClientService, HttpClientService>();

// Services
builder.Services.AddScoped<IHttpClientService, HttpClientService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

### 5. Update Docker Compose

**File**: `docker-compose.yml`

**Important Changes for .NET 9:**
- Build context changed from service-specific (e.g., `./backend/IdentityService`) to `./backend` to allow access to Shared project
- Port mappings changed from `5001:80` to `5001:8080` (.NET 9 default port)
- Volume mounts removed (they override compiled DLLs and prevent services from starting)
- Dockerfile path now relative to build context (e.g., `IdentityService/Dockerfile`)
- Replace `sri-subscription` with your actual project name (e.g., `sri_template`)

```yaml
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: sri_template_dev
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  pgadmin:
    image: dpage/pgadmin4:latest
    environment:
      PGADMIN_DEFAULT_EMAIL: admin@example.com
      PGADMIN_DEFAULT_PASSWORD: admin
    ports:
      - "5050:80"
    depends_on:
      - postgres

  identity-service:
    build:
      context: ./backend
      dockerfile: IdentityService/Dockerfile
    ports:
      - "5001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=sri_template_dev;Username=postgres;Password=postgres
    depends_on:
      postgres:
        condition: service_healthy

  data-service:
    build:
      context: ./backend
      dockerfile: DataService/Dockerfile
    ports:
      - "5003:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=sri_template_dev;Username=postgres;Password=postgres
    depends_on:
      postgres:
        condition: service_healthy

  api-gateway:
    build:
      context: ./backend
      dockerfile: ApiGateway/Dockerfile
    ports:
      - "5002:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - Services__IdentityService=http://identity-service:8080
      - Services__DataService=http://data-service:8080
    depends_on:
      - identity-service
      - data-service

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    ports:
      - "3000:3000"
    environment:
      - VITE_API_URL=http://localhost:5002
    volumes:
      - ./frontend:/app
      - /app/node_modules
    depends_on:
      - api-gateway

volumes:
  postgres_data:
```

### 6. Create Dockerfiles for Each Service

**File**: `backend/IdentityService/Dockerfile`

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Shared/Shared.csproj", "Shared/"]
COPY ["IdentityService/IdentityService.csproj", "IdentityService/"]
RUN dotnet restore "IdentityService/IdentityService.csproj"
COPY Shared/ Shared/
COPY IdentityService/ IdentityService/
WORKDIR /src/IdentityService
RUN dotnet build "IdentityService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IdentityService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.dll"]
```

**Note:** .NET 9 changed the default port from 80 to 8080. Build context is set to `./backend` in docker-compose.yml.

**File**: `backend/DataService/Dockerfile`

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Shared/Shared.csproj", "Shared/"]
COPY ["DataService/DataService.csproj", "DataService/"]
RUN dotnet restore "DataService/DataService.csproj"
COPY Shared/ Shared/
COPY DataService/ DataService/
WORKDIR /src/DataService
RUN dotnet build "DataService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DataService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DataService.dll"]
```

**File**: `backend/ApiGateway/Dockerfile`

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ApiGateway/ApiGateway.csproj", "ApiGateway/"]
RUN dotnet restore "ApiGateway/ApiGateway.csproj"
COPY ApiGateway/ ApiGateway/
WORKDIR /src/ApiGateway
RUN dotnet build "ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
```

**Note:** ApiGateway doesn't reference Shared project, so no Shared.csproj copy needed.

### 7. Update Frontend to Use Real API

**File**: `frontend/src/stores/AuthStore.ts`

```typescript
import { makeAutoObservable, runInAction } from "mobx";
import { api } from "../services/api";

export interface User {
  id: string;
  email: string;
  name: string;
}

export class AuthStore {
  user: User | null = null;
  isAuthenticated = false;
  loading = false;
  error: string | null = null;

  constructor() {
    makeAutoObservable(this);
  }

  async login(email: string, password: string): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      const response = await api.post("/auth/login", { email, password });
      const { token } = response.data;

      localStorage.setItem("token", token);

      // For now, create a mock user (will be replaced with real user data in Phase 6)
      runInAction(() => {
        this.user = { id: "1", email, name: "Test User" };
        this.isAuthenticated = true;
        this.loading = false;
      });
    } catch (error: any) {
      runInAction(() => {
        this.error = error.response?.data?.message || "Login failed";
        this.loading = false;
      });
    }
  }

  logout(): void {
    localStorage.removeItem("token");
    this.user = null;
    this.isAuthenticated = false;
  }

  clearError(): void {
    this.error = null;
  }
}
```

**File**: `frontend/src/stores/DataStore.ts`

```typescript
import { makeAutoObservable, runInAction } from "mobx";
import { api } from "../services/api";

export interface Product {
  id: string;
  name: string;
  price: number;
  description: string;
}

export class DataStore {
  products: Product[] = [];
  loading = false;
  error: string | null = null;

  constructor() {
    makeAutoObservable(this);
  }

  async fetchProducts(): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      const response = await api.get("/products");

      runInAction(() => {
        this.products = response.data;
        this.loading = false;
      });
    } catch (error: any) {
      runInAction(() => {
        this.error =
          error.response?.data?.message || "Failed to fetch products";
        this.loading = false;
      });
    }
  }
}
```

## Validation

After completing this phase, verify:

- [ ] All 3 services start successfully with `docker-compose up`
- [ ] Identity Service accessible at http://localhost:5001/swagger
- [ ] Data Service accessible at http://localhost:5003/swagger
- [ ] API Gateway accessible at http://localhost:5002/swagger
- [ ] Frontend can login via API Gateway
- [ ] Frontend can fetch products via API Gateway
- [ ] Services communicate correctly through API Gateway
- [ ] All tests pass for each service
- [ ] Health checks work for all services

## Next Steps

Proceed to [Phase 3: Database Migrations & Testing](phase3-migrations-testing.md)

## Notes

- Each service has its own database context but shares the same PostgreSQL instance
- API Gateway acts as a reverse proxy to other services
- Services communicate via HTTP (no direct database access between services)
- Shared library contains common DTOs and models
- Authentication is still mock (real auth in Phase 6)
- Each service can be developed and deployed independently





