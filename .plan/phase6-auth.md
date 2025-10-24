# Phase 6: Authentication (Cognito)

> 📖 **Quick Start?** If you've done this before, see [`phase6-quick-start.md`](phase6-quick-start.md) for a condensed version.  
> 📚 **Detailed Guide:** This document provides comprehensive code examples and explanations.

## Goal

Implement real authentication using AWS Cognito, replacing the mock authentication from previous phases. This includes frontend integration, backend JWT validation, and user management.

**Note:** This implementation uses `amazon-cognito-identity-js` (Direct SDK) instead of AWS Amplify for a lighter bundle size (~50KB vs ~300KB).

## Prerequisites

- Phase 5 completed (AWS deployment working)
- Cognito User Pool and Client created in Phase 4
- Understanding of JWT tokens and OAuth flows
- Frontend and backend services deployed

## Deliverables Checklist

### 1. Frontend Cognito Integration

#### Install AWS Amplify SDK

```bash
cd frontend
npm install aws-amplify
```

#### Configure Amplify

**File**: `frontend/src/config/amplify.ts`

```typescript
import { Amplify } from "aws-amplify";

const amplifyConfig = {
  Auth: {
    Cognito: {
      userPoolId: import.meta.env.VITE_COGNITO_USER_POOL_ID,
      userPoolClientId: import.meta.env.VITE_COGNITO_CLIENT_ID,
      loginWith: {
        email: true,
        username: false,
        phone: false,
      },
    },
  },
};

export const configureAmplify = () => {
  Amplify.configure(amplifyConfig);
};
```

#### Update Auth Store

**File**: `frontend/src/stores/AuthStore.ts`

```typescript
import { makeAutoObservable, runInAction } from "mobx";
import {
  signIn,
  signUp,
  signOut,
  getCurrentUser,
  fetchUserAttributes,
} from "aws-amplify/auth";
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
    this.initializeAuth();
  }

  private async initializeAuth(): Promise<void> {
    try {
      const user = await getCurrentUser();
      if (user) {
        const attributes = await fetchUserAttributes();
        runInAction(() => {
          this.user = {
            id: user.userId,
            email: attributes.email || "",
            name: attributes.name || attributes.email || "",
          };
          this.isAuthenticated = true;
        });
      }
    } catch (error) {
      // User not authenticated
      console.log("No authenticated user");
    }
  }

  async login(email: string, password: string): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      const result = await signIn({
        username: email,
        password: password,
      });

      if (result.isSignedIn) {
        const user = await getCurrentUser();
        const attributes = await fetchUserAttributes();

        runInAction(() => {
          this.user = {
            id: user.userId,
            email: attributes.email || "",
            name: attributes.name || attributes.email || "",
          };
          this.isAuthenticated = true;
          this.loading = false;
        });
      }
    } catch (error: any) {
      runInAction(() => {
        this.error = error.message || "Login failed";
        this.loading = false;
      });
    }
  }

  async signup(email: string, password: string, name: string): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      await signUp({
        username: email,
        password: password,
        options: {
          userAttributes: {
            email: email,
            name: name,
          },
        },
      });

      runInAction(() => {
        this.loading = false;
      });
    } catch (error: any) {
      runInAction(() => {
        this.error = error.message || "Signup failed";
        this.loading = false;
      });
    }
  }

  async logout(): Promise<void> {
    try {
      await signOut();
      runInAction(() => {
        this.user = null;
        this.isAuthenticated = false;
      });
    } catch (error) {
      console.error("Logout error:", error);
    }
  }

  clearError(): void {
    this.error = null;
  }
}
```

#### Update API Service

**File**: `frontend/src/services/api.ts`

```typescript
import axios, { AxiosInstance } from "axios";
import { fetchAuthSession } from "aws-amplify/auth";

const createApiClient = (): AxiosInstance => {
  const client = axios.create({
    baseURL: import.meta.env.VITE_API_URL || "http://localhost:5002",
    timeout: 10000,
  });

  // Request interceptor
  client.interceptors.request.use(async (config) => {
    try {
      const session = await fetchAuthSession();
      const token = session.tokens?.idToken?.toString();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
    } catch (error) {
      console.log("No auth session available");
    }
    return config;
  });

  // Response interceptor
  client.interceptors.response.use(
    (response) => response,
    (error) => {
      if (error.response?.status === 401) {
        // Handle unauthorized - redirect to login
        window.location.href = "/login";
      }
      return Promise.reject(error);
    }
  );

  return client;
};

export const api = createApiClient();
```

#### Create Signup Page

**File**: `frontend/src/pages/SignupPage.tsx`

```typescript
import React, { useState } from "react";
import { observer } from "mobx-react-lite";
import { useStore } from "../stores";
import { Link } from "react-router-dom";

export const SignupPage: React.FC = observer(() => {
  const { authStore } = useStore();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [name, setName] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (password !== confirmPassword) {
      authStore.error = "Passwords do not match";
      return;
    }

    await authStore.signup(email, password, name);
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Create your account
          </h2>
        </div>
        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div className="space-y-4">
            <div>
              <input
                type="text"
                required
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                placeholder="Full name"
              />
            </div>
            <div>
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                placeholder="Email address"
              />
            </div>
            <div>
              <input
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                placeholder="Password"
              />
            </div>
            <div>
              <input
                type="password"
                required
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                className="appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                placeholder="Confirm password"
              />
            </div>
          </div>

          {authStore.error && (
            <div className="text-red-600 text-sm">{authStore.error}</div>
          )}

          <div>
            <button
              type="submit"
              disabled={authStore.loading}
              className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50"
            >
              {authStore.loading ? "Creating account..." : "Create account"}
            </button>
          </div>

          <div className="text-center">
            <Link to="/login" className="text-indigo-600 hover:text-indigo-500">
              Already have an account? Sign in
            </Link>
          </div>
        </form>
      </div>
    </div>
  );
});
```

#### Update App Router

**File**: `frontend/src/App.tsx`

```typescript
import React, { useEffect } from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { useStore } from "./stores";
import { configureAmplify } from "./config/amplify";
import { LoginPage } from "./pages/LoginPage";
import { SignupPage } from "./pages/SignupPage";
import { DashboardPage } from "./pages/DashboardPage";

// Configure Amplify
configureAmplify();

const ProtectedRoute: React.FC<{ children: React.ReactElement }> = ({
  children,
}) => {
  const { authStore } = useStore();
  return authStore.isAuthenticated ? children : <Navigate to="/login" />;
};

export const App: React.FC = observer(() => {
  const { authStore } = useStore();

  useEffect(() => {
    // Initialize auth state
    authStore.initializeAuth();
  }, []);

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/signup" element={<SignupPage />} />
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <DashboardPage />
            </ProtectedRoute>
          }
        />
        <Route path="/" element={<Navigate to="/dashboard" />} />
      </Routes>
    </BrowserRouter>
  );
});
```

### 2. Backend JWT Validation

#### Install JWT Packages

```bash
cd backend/IdentityService
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.IdentityModel.Tokens
```

#### Create JWT Authentication Service

**File**: `backend/Shared/Services/IJwtService.cs`

```csharp
using System.Security.Claims;

namespace Shared.Services;

public interface IJwtService
{
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
    Task<string?> GetUserIdAsync(ClaimsPrincipal principal);
}
```

**File**: `backend/Shared/Services/CognitoJwtService.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace Shared.Services;

public class CognitoJwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CognitoJwtService> _logger;
    private readonly string _userPoolId;
    private readonly string _region;

    public CognitoJwtService(IConfiguration configuration, ILogger<CognitoJwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _userPoolId = _configuration["Cognito:UserPoolId"] ?? throw new InvalidOperationException("Cognito:UserPoolId not configured");
        _region = _configuration["Cognito:Region"] ?? throw new InvalidOperationException("Cognito:Region not configured");
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            var jwtHandler = new JwtSecurityTokenHandler();
            var jsonToken = jwtHandler.ReadJwtToken(token);

            // Get Cognito public keys
            var publicKeys = await GetCognitoPublicKeysAsync();
            var key = publicKeys[jsonToken.Header.Kid];

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://cognito-idp.{_region}.amazonaws.com/{_userPoolId}",
                ValidateAudience = true,
                ValidAudience = _configuration["Cognito:ClientId"],
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = jwtHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("JWT validation failed: {Error}", ex.Message);
            return null;
        }
    }

    public async Task<string?> GetUserIdAsync(ClaimsPrincipal principal)
    {
        return principal.FindFirst("sub")?.Value;
    }

    private async Task<Dictionary<string, SecurityKey>> GetCognitoPublicKeysAsync()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync($"https://cognito-idp.{_region}.amazonaws.com/{_userPoolId}/.well-known/jwks.json");
        var jwks = JsonSerializer.Deserialize<JsonWebKeySet>(response);

        var keys = new Dictionary<string, SecurityKey>();
        foreach (var key in jwks.Keys)
        {
            var rsa = System.Security.Cryptography.RSA.Create();
            rsa.ImportParameters(new System.Security.Cryptography.RSAParameters
            {
                Modulus = Base64UrlDecode(key.N),
                Exponent = Base64UrlDecode(key.E)
            });

            keys[key.Kid] = new RsaSecurityKey(rsa);
        }

        return keys;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        input = input.Replace('-', '+').Replace('_', '/');
        switch (input.Length % 4)
        {
            case 2: input += "=="; break;
            case 3: input += "="; break;
        }
        return Convert.FromBase64String(input);
    }
}
```

#### Create Authentication Middleware

**File**: `backend/Shared/Middleware/JwtAuthenticationMiddleware.cs`

```csharp
using Shared.Services;

namespace Shared.Middleware;

public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    public JwtAuthenticationMiddleware(RequestDelegate next, ILogger<JwtAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
    {
        var token = ExtractTokenFromHeader(context.Request);

        if (!string.IsNullOrEmpty(token))
        {
            var principal = await jwtService.ValidateTokenAsync(token);
            if (principal != null)
            {
                context.User = principal;
            }
        }

        await _next(context);
    }

    private static string? ExtractTokenFromHeader(HttpRequest request)
    {
        var authHeader = request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }
        return null;
    }
}
```

#### Update Identity Service Program.cs

**File**: `backend/IdentityService/Program.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using IdentityService.Data;
using IdentityService.Services;
using Shared.Services;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtService, CognitoJwtService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(builder.Configuration["FrontendUrl"] ?? "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
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
app.UseCors("AllowFrontend");

// Add JWT authentication middleware
app.UseMiddleware<JwtAuthenticationMiddleware>();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

#### Update Identity Service Controllers

**File**: `backend/IdentityService/Controllers/UsersController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;
using IdentityService.Services;
using System.Security.Claims;

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

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userService.GetUserByIdAsync(userId, cancellationToken);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
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
}
```

### 3. Update API Gateway

#### Update API Gateway Controllers

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

    [HttpPost("signup")]
    public async Task<IActionResult> Signup(
        [FromBody] object dto,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClientService.PostAsync("identity", "api/v1/auth/signup", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return StatusCode((int)response.StatusCode, responseContent);
    }
}
```

### 4. Update Data Service

#### Update Data Service Controllers

**File**: `backend/DataService/Controllers/ProductsController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;
using DataService.Services;
using System.Security.Claims;

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

### 5. Update Environment Variables

#### Frontend Environment Variables

**File**: `frontend/.env.example`

```
VITE_API_URL=https://api-gateway.sri-subscription.com
VITE_COGNITO_USER_POOL_ID={{COGNITO_USER_POOL_ID}}
VITE_COGNITO_CLIENT_ID={{COGNITO_USER_POOL_CLIENT_ID}}
VITE_COGNITO_DOMAIN={{COGNITO_DOMAIN}}
```

#### Backend Environment Variables

**File**: `backend/IdentityService/.env.example`

```
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=sri-subscription_dev;Username=postgres;Password=postgres
Cognito__UserPoolId={{COGNITO_USER_POOL_ID}}
Cognito__ClientId={{COGNITO_USER_POOL_CLIENT_ID}}
Cognito__Region=us-east-1
ASPNETCORE_ENVIRONMENT=Development
```

### 6. Update Docker Compose for Local Development

**File**: `docker-compose.override.yml`

```yaml
# Override for local development with Cognito
version: "3.8"

services:
  identity-service:
    environment:
      - Cognito__UserPoolId=${COGNITO_USER_POOL_ID}
      - Cognito__ClientId=${COGNITO_USER_POOL_CLIENT_ID}
      - Cognito__Region=${AWS_REGION}

  api-gateway:
    environment:
      - Services__IdentityService=http://identity-service:80
      - Services__DataService=http://data-service:80

  frontend:
    environment:
      - VITE_API_URL=http://localhost:5002
      - VITE_COGNITO_USER_POOL_ID=${COGNITO_USER_POOL_ID}
      - VITE_COGNITO_CLIENT_ID=${COGNITO_USER_POOL_CLIENT_ID}
      - VITE_COGNITO_DOMAIN=${COGNITO_DOMAIN}
```

### 7. Update Deployment Scripts

#### Update Build Script

**File**: `scripts/build-and-push.ps1`

```powershell
# Build and push Docker images to ECR
param(
    [string]$Environment = "dev"
)

Write-Host "🔨 Building Docker images..." -ForegroundColor Yellow

# Get ECR repository URLs from GitHub secrets or environment variables
$ecrIdentity = $env:ECR_IDENTITY_SERVICE_URL
$ecrGateway = $env:ECR_API_GATEWAY_URL
$ecrData = $env:ECR_DATA_SERVICE_URL
$ecrFrontend = $env:ECR_FRONTEND_URL

# Login to ECR
Write-Host "🔐 Logging in to ECR..." -ForegroundColor Yellow
aws ecr get-login-password --region $env:AWS_REGION | docker login --username AWS --password-stdin $ecrIdentity

# Build and push Identity Service
Write-Host "Building Identity Service..." -ForegroundColor Yellow
docker build -t identity-service ./backend/IdentityService
docker tag identity-service:latest "$ecrIdentity:latest"
docker push "$ecrIdentity:latest"

# Build and push API Gateway
Write-Host "Building API Gateway..." -ForegroundColor Yellow
docker build -t api-gateway ./backend/ApiGateway
docker tag api-gateway:latest "$ecrGateway:latest"
docker push "$ecrGateway:latest"

# Build and push Data Service
Write-Host "Building Data Service..." -ForegroundColor Yellow
docker build -t data-service ./backend/DataService
docker tag data-service:latest "$ecrData:latest"
docker push "$ecrData:latest"

# Build and push Frontend
Write-Host "Building Frontend..." -ForegroundColor Yellow
docker build -t frontend ./frontend
docker tag frontend:latest "$ecrFrontend:latest"
docker push "$ecrFrontend:latest"

Write-Host "✅ All images built and pushed!" -ForegroundColor Green
```

## Validation

After completing this phase, verify:

- [ ] Frontend can sign up new users
- [ ] Frontend can sign in existing users
- [ ] JWT tokens are properly validated
- [ ] Protected routes require authentication
- [ ] Backend services can extract user information
- [ ] API calls include proper authorization headers
- [ ] Logout functionality works
- [ ] Error handling for invalid tokens
- [ ] Local development with Cognito works
- [ ] Production deployment with Cognito works

## Next Steps

Proceed to [Phase 7: CI/CD Pipeline](phase7-cicd.md)

## Notes

- Cognito handles user management and authentication
- JWT tokens are validated using Cognito public keys
- Frontend uses AWS Amplify for easy integration
- Backend services extract user information from JWT claims
- Local development can use real Cognito or mock authentication
- Production deployment uses real Cognito authentication
- All services are secured with proper authentication
- User experience is seamless with automatic token refresh





