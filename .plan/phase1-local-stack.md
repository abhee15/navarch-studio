# Phase 1: Local Development Stack

## Goal

Get a working full-stack app running locally with docker-compose. This phase focuses on creating a monolith backend that we'll split into microservices later.

## Prerequisites

- Phase 0 completed (Git setup, folder structure)
- Node.js 20+ installed
- .NET 9 SDK installed
- Docker Desktop running

## Deliverables Checklist

### 1. React Frontend Setup

#### Create Vite + TypeScript Project

```bash
cd frontend
npm create vite@latest . -- --template react-ts
npm install
```

#### Install Dependencies

```bash
# State management
npm install mobx mobx-react-lite

# HTTP client
npm install axios

# Styling (Tailwind CSS v4+ requires @tailwindcss/postcss)
npm install -D tailwindcss postcss autoprefixer @tailwindcss/postcss
npx tailwindcss init -p

# Routing
npm install react-router-dom

# Testing
npm install -D jest @testing-library/react @testing-library/jest-dom @testing-library/user-event
npm install -D @types/jest jest-environment-jsdom ts-jest

# Linting
npm install -D eslint @typescript-eslint/parser @typescript-eslint/eslint-plugin
npm install -D eslint-plugin-react eslint-plugin-react-hooks
npm install -D prettier eslint-config-prettier eslint-plugin-prettier
```

#### Configure TypeScript (tsconfig.json)

```json
{
  "compilerOptions": {
    "target": "ES2020",
    "useDefineForClassFields": true,
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noEmit": true,
    "jsx": "react-jsx",
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true,
    "esModuleInterop": true,
    "baseUrl": ".",
    "paths": {
      "@/*": ["./src/*"]
    }
  },
  "include": ["src"]
}
```

#### Configure TypeScript for Node (tsconfig.node.json)

```json
{
  "compilerOptions": {
    "target": "ES2023",
    "lib": ["ES2023"],
    "module": "ESNext",
    "types": ["node"],
    "skipLibCheck": true,
    "noEmit": true,
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "verbatimModuleSyntax": true,
    "moduleDetection": "force",
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "erasableSyntaxOnly": true,
    "noFallthroughCasesInSwitch": true,
    "noUncheckedSideEffectImports": true
  },
  "include": ["vite.config.ts"]
}
```

#### Configure TailwindCSS (tailwind.config.js)

```javascript
/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
  theme: {
    extend: {},
  },
  plugins: [],
};
```

#### Configure PostCSS (postcss.config.js)

**Note**: Tailwind CSS v4+ requires the `@tailwindcss/postcss` package.

```javascript
export default {
  plugins: {
    "@tailwindcss/postcss": {},
    autoprefixer: {},
  },
};
```

#### Configure ESLint (eslint.config.js)

**Note**: Using flat config format (ESLint 9+).

```javascript
import js from "@eslint/js";
import globals from "globals";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import tseslint from "typescript-eslint";
import { defineConfig, globalIgnores } from "eslint/config";

export default defineConfig([
  globalIgnores(["dist"]),
  {
    files: ["**/*.{ts,tsx}"],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs["recommended-latest"],
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    rules: {
      "@typescript-eslint/no-unused-vars": [
        "error",
        { argsIgnorePattern: "^_" },
      ],
    },
  },
]);
```

#### Configure Jest (jest.config.js)

```javascript
export default {
  preset: "ts-jest",
  testEnvironment: "jsdom",
  setupFilesAfterEnv: ["<rootDir>/src/setupTests.ts"],
  moduleNameMapper: {
    "^@/(.*)$": "<rootDir>/src/$1",
  },
  collectCoverageFrom: ["src/**/*.{ts,tsx}", "!src/**/*.d.ts", "!src/main.tsx"],
};
```

#### Create MobX Stores

**File**: `src/stores/AuthStore.ts`

```typescript
import { makeAutoObservable, runInAction } from "mobx";

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

  async login(email: string, _password: string): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      // Mock login for now
      await new Promise((resolve) => setTimeout(resolve, 1000));

      runInAction(() => {
        this.user = { id: "1", email, name: "Test User" };
        this.isAuthenticated = true;
        this.loading = false;
      });
    } catch {
      runInAction(() => {
        this.error = "Login failed";
        this.loading = false;
      });
    }
  }

  logout(): void {
    this.user = null;
    this.isAuthenticated = false;
  }

  clearError(): void {
    this.error = null;
  }
}
```

**File**: `src/stores/DataStore.ts`

```typescript
import { makeAutoObservable, runInAction } from "mobx";

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
      // Mock data for now
      const mockProducts: Product[] = [
        {
          id: "1",
          name: "Product 1",
          price: 29.99,
          description: "Description 1",
        },
        {
          id: "2",
          name: "Product 2",
          price: 39.99,
          description: "Description 2",
        },
      ];

      await new Promise((resolve) => setTimeout(resolve, 1000));

      runInAction(() => {
        this.products = mockProducts;
        this.loading = false;
      });
    } catch {
      runInAction(() => {
        this.error = "Failed to fetch products";
        this.loading = false;
      });
    }
  }
}
```

#### Create API Service

**File**: `src/services/api.ts`

```typescript
import axios, { AxiosInstance } from "axios";

const createApiClient = (): AxiosInstance => {
  const client = axios.create({
    baseURL: import.meta.env.VITE_API_URL || "http://localhost:5002",
    timeout: 10000,
  });

  // Request interceptor
  client.interceptors.request.use((config) => {
    const token = localStorage.getItem("token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  });

  // Response interceptor
  client.interceptors.response.use(
    (response) => response,
    (error) => {
      if (error.response?.status === 401) {
        localStorage.removeItem("token");
        window.location.href = "/login";
      }
      return Promise.reject(error);
    }
  );

  return client;
};

export const api = createApiClient();
```

#### Create Pages

**File**: `src/pages/LoginPage.tsx`

```typescript
import React, { useState } from "react";
import { observer } from "mobx-react-lite";
import { useStore } from "../stores";

export const LoginPage: React.FC = observer(() => {
  const { authStore } = useStore();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await authStore.login(email, password);
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Sign in to your account
          </h2>
        </div>
        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div className="rounded-md shadow-sm -space-y-px">
            <div>
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-t-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                placeholder="Email address"
              />
            </div>
            <div>
              <input
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-b-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                placeholder="Password"
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
              {authStore.loading ? "Signing in..." : "Sign in"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
});
```

**File**: `src/pages/DashboardPage.tsx`

```typescript
import React, { useEffect } from "react";
import { observer } from "mobx-react-lite";
import { useStore } from "../stores";

export const DashboardPage: React.FC = observer(() => {
  const { authStore, dataStore } = useStore();

  useEffect(() => {
    dataStore.fetchProducts();
  }, []);

  const handleLogout = () => {
    authStore.logout();
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <h1 className="text-xl font-semibold">Dashboard</h1>
            </div>
            <div className="flex items-center space-x-4">
              <span className="text-gray-700">
                Welcome, {authStore.user?.name}
              </span>
              <button
                onClick={handleLogout}
                className="bg-red-600 text-white px-4 py-2 rounded-md hover:bg-red-700"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </nav>

      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          <h2 className="text-2xl font-bold text-gray-900 mb-6">Products</h2>

          {dataStore.loading && (
            <div className="text-center py-8">Loading products...</div>
          )}

          {dataStore.error && (
            <div className="text-red-600 py-8">{dataStore.error}</div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {dataStore.products.map((product) => (
              <div key={product.id} className="bg-white rounded-lg shadow p-6">
                <h3 className="text-lg font-semibold text-gray-900">
                  {product.name}
                </h3>
                <p className="text-gray-600 mt-2">{product.description}</p>
                <p className="text-2xl font-bold text-indigo-600 mt-4">
                  ${product.price}
                </p>
              </div>
            ))}
          </div>
        </div>
      </main>
    </div>
  );
});
```

#### Create App Router

**File**: `src/App.tsx`

```typescript
import React from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { useStore } from "./stores";
import { LoginPage } from "./pages/LoginPage";
import { DashboardPage } from "./pages/DashboardPage";

const ProtectedRoute: React.FC<{ children: React.ReactElement }> = ({
  children,
}) => {
  const { authStore } = useStore();
  return authStore.isAuthenticated ? children : <Navigate to="/login" />;
};

export const App: React.FC = observer(() => (
  <BrowserRouter>
    <Routes>
      <Route path="/login" element={<LoginPage />} />
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
));
```

#### Create Store Provider

**File**: `src/stores/index.ts`

```typescript
import { AuthStore } from "./AuthStore";
import { DataStore } from "./DataStore";

export class RootStore {
  authStore: AuthStore;
  dataStore: DataStore;

  constructor() {
    this.authStore = new AuthStore();
    this.dataStore = new DataStore();
  }
}

export const rootStore = new RootStore();
export const useStore = () => rootStore;
```

#### Environment Variables

**File**: `.env.example`

```
VITE_API_URL=http://localhost:5002
VITE_APP_NAME=sri-subscription
```

### 2. .NET Backend (Monolith)

#### Create Solution and Project

```bash
cd backend
dotnet new sln -n sri-subscription
dotnet new webapi -n ApiService --framework net9.0
dotnet sln add ApiService
```

#### Install NuGet Packages

```bash
cd ApiService
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Swashbuckle.AspNetCore
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package BCrypt.Net-Next
dotnet add package AspNetCore.HealthChecks.NpgSql
```

#### Create Models

**File**: `Models/User.cs`

```csharp
namespace ApiService.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

**File**: `Models/Product.cs`

```csharp
namespace ApiService.Models;

public class Product
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

#### Create DbContext

**File**: `Data/ApplicationDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using ApiService.Models;

namespace ApiService.Data;

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

#### Create Controllers

**File**: `Controllers/UsersController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using ApiService.Data;
using ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(ApplicationDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(string id, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }
}
```

**File**: `Controllers/ProductsController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using ApiService.Data;
using ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts(CancellationToken cancellationToken)
    {
        var products = await _context.Products
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(string id, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null)
        {
            return NotFound();
        }

        return Ok(product);
    }
}
```

#### Configure Program.cs

```csharp
using Microsoft.EntityFrameworkCore;
using ApiService.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

#### Create appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=sri-subscription_dev;Username=postgres;Password=postgres"
  }
}
```

**Note**: In the current implementation, replace `sri-subscription_dev` with `sri_template_dev`.

### 3. Docker Compose Setup

**File**: `docker-compose.yml`

**Note**: In the current implementation, use `sri_template_dev` instead of `sri-subscription_dev`.

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

  api:
    build:
      context: ./backend/ApiService
      dockerfile: Dockerfile
      target: development
    ports:
      - "5002:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=sri_template_dev;Username=postgres;Password=postgres
    depends_on:
      postgres:
        condition: service_healthy
    volumes:
      - ./backend/ApiService:/app
      - /app/bin
      - /app/obj

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
      target: dev
    ports:
      - "3000:3000"
    environment:
      - VITE_API_URL=http://localhost:5002
    volumes:
      - ./frontend:/app
      - /app/node_modules
    depends_on:
      - api

volumes:
  postgres_data:
```

### 4. Dockerfiles

**File**: `backend/ApiService/Dockerfile`

**Note**: Multi-stage Dockerfile with separate development and production stages. Development stage enables hot reload.

```dockerfile
# Development stage for hot reload
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development
WORKDIR /app
EXPOSE 8080
# Install dev tools
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"
# Entry point for development with hot reload
ENTRYPOINT ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:8080"]

# Production build stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ApiService.csproj", "."]
RUN dotnet restore "ApiService.csproj"
COPY . .
RUN dotnet build "ApiService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ApiService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiService.dll"]
```

**File**: `frontend/Dockerfile`

```dockerfile
FROM node:20-alpine AS base
WORKDIR /app

FROM base AS deps
COPY package*.json ./
RUN npm ci

FROM base AS dev
COPY --from=deps /app/node_modules ./node_modules
COPY . .
EXPOSE 3000
CMD ["npm", "run", "dev", "--", "--host"]
```

### 5. Testing Setup

#### Frontend Tests

**File**: `src/setupTests.ts`

```typescript
import "@testing-library/jest-dom";
```

**File**: `src/stores/__tests__/AuthStore.test.ts`

```typescript
import { AuthStore } from "../AuthStore";

describe("AuthStore", () => {
  let authStore: AuthStore;

  beforeEach(() => {
    authStore = new AuthStore();
  });

  it("should initialize with default values", () => {
    expect(authStore.user).toBeNull();
    expect(authStore.isAuthenticated).toBe(false);
    expect(authStore.loading).toBe(false);
    expect(authStore.error).toBeNull();
  });

  it("should login successfully", async () => {
    await authStore.login("test@example.com", "password");

    expect(authStore.isAuthenticated).toBe(true);
    expect(authStore.user).toBeDefined();
    expect(authStore.user?.email).toBe("test@example.com");
  });

  it("should logout", () => {
    authStore.user = { id: "1", email: "test@example.com", name: "Test" };
    authStore.isAuthenticated = true;

    authStore.logout();

    expect(authStore.user).toBeNull();
    expect(authStore.isAuthenticated).toBe(false);
  });
});
```

#### Backend Tests

```bash
cd backend
dotnet new xunit -n ApiService.Tests
dotnet sln add ApiService.Tests
cd ApiService.Tests
dotnet add reference ../ApiService
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Moq
```

**File**: `backend/ApiService.Tests/Controllers/UsersControllerTests.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ApiService.Controllers;
using ApiService.Data;
using ApiService.Models;

namespace ApiService.Tests.Controllers;

public class UsersControllerTests
{
    private readonly ApplicationDbContext _context;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _controller = new UsersController(_context, Mock.Of<ILogger<UsersController>>());
    }

    [Fact]
    public async Task GetUsers_ReturnsAllUsers()
    {
        // Arrange
        _context.Users.Add(new User { Id = "1", Email = "test@example.com", Name = "Test" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetUsers(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IEnumerable<User>>(okResult.Value);
        Assert.Single(users);
    }
}
```

### 6. Database Migrations

After starting Docker Compose for the first time, you need to create and apply database migrations:

```bash
# Create initial migration
docker compose exec api dotnet ef migrations add InitialCreate

# Apply migration to create database tables
docker compose exec api dotnet ef database update
```

**Note**: This step is required before the API can query the database. Without it, you'll see "relation does not exist" errors.

**Alternative for local development** (if running .NET locally):

```bash
cd backend/ApiService
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Running the Application

This section provides detailed instructions for running the Phase 1 application using either Docker Compose (recommended) or local development setup.

### Option 1: Docker Compose (Recommended)

Docker Compose is the **easiest way** to run the complete stack. All dependencies are containerized, no local installations required (except Docker Desktop).

#### Prerequisites

- Docker Desktop installed and running
- At least 4GB RAM allocated to Docker
- Ports available: 3000, 5002, 5432, 5050

#### Step-by-Step Setup

**1. Start all services:**

```bash
# From project root
docker compose up --build

# Or run in detached mode (background)
docker compose up --build -d
```

**What happens:**

- PostgreSQL database starts and becomes healthy
- Backend API builds and starts (with hot reload)
- Frontend builds and starts (with hot reload)
- PgAdmin starts for database management

**2. Apply database migrations** (first time only):

```bash
# Wait 30 seconds for services to fully start, then:
docker compose exec api dotnet ef database update
```

**3. Access the application:**

- **Frontend:** http://localhost:3000
- **Backend API (Swagger):** http://localhost:5002/swagger
- **Health Check:** http://localhost:5002/health
- **PgAdmin:** http://localhost:5050
  - Email: `admin@example.com`
  - Password: `admin`

#### Managing Docker Compose

**View logs:**

```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f api
docker compose logs -f frontend
docker compose logs -f postgres
```

**Check service status:**

```bash
docker compose ps
```

**Stop services:**

```bash
# Stop and remove containers (data persists)
docker compose down

# Stop and remove containers + volumes (deletes all data)
docker compose down -v
```

**Restart a single service:**

```bash
docker compose restart api
docker compose restart frontend
```

#### Hot Reload

Both frontend and backend support hot reload:

- **Frontend:** Edit files in `frontend/src/` - browser auto-refreshes
- **Backend:** Edit `.cs` files - API automatically rebuilds and restarts

#### Data Persistence

PostgreSQL data persists in a Docker volume (`postgres_data`):

- `docker compose down` - **Keeps data** ✅
- `docker compose down -v` - **Deletes data** ❌

To see volumes:

```bash
docker volume ls
```

---

### Option 2: Local Development

Run services directly on your machine without Docker (except for PostgreSQL database).

#### Prerequisites

Install the following on your local machine:

**1. Node.js 20+**

```bash
# Download from https://nodejs.org/
node --version  # Should be v20.x or higher
npm --version
```

**2. .NET 9 SDK**

```bash
# Download from https://dotnet.microsoft.com/download
dotnet --version  # Should be 9.0.x
```

**3. Entity Framework Core Tools**

```bash
# Install globally
dotnet tool install --global dotnet-ef

# Verify installation
dotnet ef --version  # Should be 9.0.x
```

**4. PostgreSQL Database**

Choose one of these options:

**Option A: PostgreSQL in Docker** (Recommended)

```bash
docker run -d \
  --name postgres-dev \
  -e POSTGRES_DB=sri_template_dev \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  postgres:15

# Stop when done
docker stop postgres-dev

# Start again
docker start postgres-dev
```

**Option B: PostgreSQL Installed Locally**

- Download from: https://www.postgresql.org/download/
- Create database: `sri_template_dev`
- Username: `postgres`
- Password: `postgres`

#### Step-by-Step Setup

**1. Install dependencies:**

```bash
# Frontend
cd frontend
npm install

# Backend (restore NuGet packages)
cd ../backend/ApiService
dotnet restore
```

**2. Apply database migrations:**

```bash
# Make sure PostgreSQL is running, then:
cd backend/ApiService
dotnet ef database update
```

**3. Start the backend** (Terminal 1):

```bash
cd backend/ApiService
dotnet run

# You should see:
# Now listening on: http://localhost:5002
```

**4. Start the frontend** (Terminal 2):

```bash
cd frontend
npm run dev

# You should see:
# VITE ready in XXXms
# Local: http://localhost:3000/
```

**5. Access the application:**

- **Frontend:** http://localhost:3000
- **Backend API (Swagger):** http://localhost:5002/swagger
- **Health Check:** http://localhost:5002/health

#### Hot Reload (Local Development)

- **Frontend:** Vite automatically reloads on file changes
- **Backend:** Use `dotnet watch run` instead of `dotnet run` for hot reload:
  ```bash
  cd backend/ApiService
  dotnet watch run
  ```

#### Stopping Local Services

- Press `Ctrl+C` in each terminal to stop frontend/backend
- Stop PostgreSQL:

  ```bash
  # If using Docker
  docker stop postgres-dev

  # If installed locally
  # Use pgAdmin or system services manager
  ```

---

### Which Option Should I Use?

| Consideration      | Docker Compose             | Local Development                       |
| ------------------ | -------------------------- | --------------------------------------- |
| **Setup Time**     | 5 minutes                  | 15-30 minutes                           |
| **Prerequisites**  | Docker Desktop only        | Node.js, .NET, PostgreSQL, EF Tools     |
| **Consistency**    | ✅ Identical to production | ⚠️ Depends on local setup               |
| **Resource Usage** | Higher (4+ GB RAM)         | Lower (1-2 GB RAM)                      |
| **Hot Reload**     | ✅ Supported               | ✅ Supported                            |
| **Best For**       | Quick start, testing       | Active development, debugging           |
| **Port Conflicts** | Less likely                | More likely (if other services running) |

**Recommendation:**

- **Starting out or testing?** Use Docker Compose
- **Heavy development or debugging?** Use Local Development
- **Team collaboration?** Use Docker Compose (ensures consistency)

---

## Validation

After completing this phase, verify:

- [ ] `docker compose up --build` starts all services
- [ ] Database migrations applied (`docker compose exec api dotnet ef database update`)
- [ ] Frontend accessible at http://localhost:3000
- [ ] API accessible at http://localhost:5002/swagger
- [ ] API health check responds: http://localhost:5002/health
- [ ] PgAdmin accessible at http://localhost:5050 (login: admin@example.com / admin)
- [ ] Login flow works (mock authentication - any email/password)
- [ ] Dashboard shows 2 mock products after login
- [ ] All frontend tests pass (`npm test` in frontend directory)
- [ ] All backend tests pass (`dotnet test` in backend directory)
- [ ] ESLint passes (`npm run lint`)
- [ ] TypeScript type checking passes (`npm run type-check`)

### Testing the Full Stack

**Quick test sequence:**

```bash
# 1. Start services
docker compose up --build

# Wait ~30 seconds for services to start, then in a new terminal:

# 2. Apply migrations (first time only)
docker compose exec api dotnet ef database update

# 3. Test API endpoints
curl http://localhost:5002/health
curl http://localhost:5002/api/v1/products

# 4. Open in browser
# - http://localhost:3000 (should redirect to /login)
# - Enter any email/password to login
# - Should see dashboard with welcome message

# 5. Stop services
docker compose down
```

## Next Steps

Proceed to [Phase 2: Microservices Split](phase2-microservices.md)

## Notes

- This phase creates a working monolith that we'll split in Phase 2
- Mock authentication is used for now (real auth in Phase 6)
- The development Dockerfile enables hot reload for both frontend and backend
- All services run in Docker for consistency across development environments
- Database tables are created via Entity Framework migrations (not seeded with data yet)
- The `sri-subscription` placeholder will be replaced by the clone script in Phase 9

## Troubleshooting

This section covers common issues and their solutions for Phase 1.

### Quick Diagnostics

Run these commands to check service health:

```bash
# Check Docker services status
docker compose ps

# View logs for all services
docker compose logs

# Check specific service logs
docker compose logs api
docker compose logs frontend
docker compose logs postgres

# Test API connectivity
curl http://localhost:5002/health

# Test database connectivity
docker compose exec postgres pg_isready -U postgres

# List running containers
docker ps

# Check port usage (Windows PowerShell)
Get-NetTCPConnection | Where-Object {$_.LocalPort -in 3000,5002,5432,5050}

# Check port usage (Linux/Mac)
lsof -i :3000
lsof -i :5002
lsof -i :5432
lsof -i :5050
```

---

### Common Issues

#### 1. Port Already in Use

**Error:** `Bind for 0.0.0.0:5432 failed: port is already allocated`

**Solutions:**

**Find what's using the port:**

```bash
# Windows PowerShell
Get-Process -Id (Get-NetTCPConnection -LocalPort 5432).OwningProcess

# Linux/Mac
lsof -i :5432
```

**Fix:**

```bash
# Stop standalone PostgreSQL container
docker stop postgres-dev
docker rm postgres-dev

# Or stop local PostgreSQL service (Windows)
Stop-Service postgresql-x64-*

# Or stop local PostgreSQL (Linux)
sudo systemctl stop postgresql
```

**Ports used by this stack:**

- `3000` - Frontend (Vite)
- `5002` - Backend API
- `5432` - PostgreSQL
- `5050` - PgAdmin

---

#### 2. Entity Framework Tools Not Found

**Error:** `Could not execute because the specified command or file was not found: dotnet-ef`

**Solution:**

```bash
# Install EF Core tools globally
dotnet tool install --global dotnet-ef

# Verify installation
dotnet ef --version

# If still not found, add to PATH (Windows)
# Add: %USERPROFILE%\.dotnet\tools

# Linux/Mac - add to ~/.bashrc or ~/.zshrc
export PATH="$PATH:$HOME/.dotnet/tools"
```

**Alternative for Docker Compose users:**

```bash
# Run migrations inside the container (tools already installed)
docker compose exec api dotnet ef database update
```

---

#### 3. Frontend PostCSS Error

**Error:** `[postcss] It looks like you're trying to use tailwindcss directly as a PostCSS plugin`

**Solution:**

```bash
# Install the required package
cd frontend
npm install -D @tailwindcss/postcss

# Verify postcss.config.js has:
# '@tailwindcss/postcss': {}
# NOT 'tailwindcss': {}
```

---

#### 4. Backend Container Won't Start

**Error:** API container exits immediately or shows "ApiService.dll does not exist"

**Solutions:**

**Check logs:**

```bash
docker compose logs api
```

**Verify docker-compose.yml has:**

```yaml
api:
  build:
    target: development # This line is critical!
```

**Rebuild without cache:**

```bash
docker compose down
docker compose build --no-cache api
docker compose up api
```

---

#### 5. Database Connection Errors

**Error:** `Npgsql.PostgresException: relation "Products" does not exist`

**Cause:** Migrations not applied

**Solution:**

```bash
# Apply migrations
docker compose exec api dotnet ef database update

# Verify tables exist
docker compose exec postgres psql -U postgres -d sri_template_dev -c "\dt"
```

---

**Error:** `Connection refused` or `could not connect to server`

**Solutions:**

**Check PostgreSQL is running:**

```bash
docker compose ps postgres
# Should show "healthy" status
```

**Wait for PostgreSQL to be ready:**

```bash
# PostgreSQL takes 10-15 seconds to fully start
docker compose logs postgres | grep "ready to accept connections"
```

**Verify connection string:**

- Docker Compose: Host should be `postgres` (service name)
- Local dev: Host should be `localhost`

---

#### 6. PgAdmin Connection Issues

**Error:** Can't connect to PostgreSQL from PgAdmin

**Solution - Use these settings:**

- **Host:** `postgres` (not `localhost`!)
- **Port:** `5432`
- **Username:** `postgres`
- **Password:** `postgres`
- **Database:** `sri_template_dev` or `sri-subscription_dev`

**Why `postgres` not `localhost`?**
Inside Docker networks, services communicate using their service names, not localhost.

---

#### 7. Node Modules Issues

**Error:** `Cannot find module` or module resolution errors

**Solutions:**

```bash
# Delete and reinstall
cd frontend
rm -rf node_modules package-lock.json
npm install

# For Docker, rebuild
docker compose down
docker compose build --no-cache frontend
docker compose up frontend
```

---

#### 8. Docker Build Failures

**Error:** `failed to compute cache key` or build context errors

**Solutions:**

**Clear Docker build cache:**

```bash
docker system prune -a
docker volume prune
```

**Check .dockerignore exists:**

```bash
ls -la frontend/.dockerignore
ls -la backend/ApiService/.dockerignore
```

**Rebuild from scratch:**

```bash
docker compose down -v
docker compose build --no-cache
docker compose up
```

---

#### 9. Hot Reload Not Working

**Frontend hot reload not working:**

```bash
# Check volume mounts in docker-compose.yml:
volumes:
  - ./frontend:/app
  - /app/node_modules  # Must exclude node_modules
```

**Backend hot reload not working:**

- Verify Dockerfile uses `dotnet watch run`
- Check `target: development` in docker-compose.yml
- Try: `docker compose restart api`

---

#### 10. TypeScript Compilation Errors

**Error:** `Option 'allowImportingTsExtensions' can only be used when...`

**Solution:**

```bash
# Verify tsconfig.node.json has:
# "noEmit": true

# Verify tsconfig.json does NOT have:
# "references": [{ "path": "./tsconfig.node.json" }]
```

**Error:** `'password' is defined but never used`

**Solution:**
Prefix unused parameters with underscore: `_password`

Or add to eslint.config.js:

```javascript
rules: {
  '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
}
```

---

### Platform-Specific Issues

#### Windows

**Line endings causing issues:**

```bash
# Configure git to handle line endings
git config --global core.autocrlf true
```

**PowerShell execution policy:**

```powershell
# If scripts won't run
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

**WSL2 Docker performance:**

- Keep project files in WSL2 filesystem, not Windows drives
- Good: `/home/user/projects`
- Bad: `/mnt/c/Users/...`

#### macOS

**Docker Desktop memory:**

- Go to Docker Desktop → Settings → Resources
- Increase memory to at least 4GB

**Port binding on M1/M2:**

- Use `127.0.0.1` instead of `localhost` if having issues
- Example: `http://127.0.0.1:3000`

#### Linux

**Docker permission errors:**

```bash
# Add user to docker group
sudo usermod -aG docker $USER
newgrp docker
```

**Volume permission issues:**

```bash
# If files created by Docker have wrong permissions
sudo chown -R $USER:$USER frontend backend
```

---

### Database-Specific Troubleshooting

**Reset database completely:**

```bash
# Stop services and delete volumes
docker compose down -v

# Start fresh
docker compose up -d postgres
sleep 10  # Wait for PostgreSQL

# Recreate migrations
docker compose exec api dotnet ef database update
```

**Inspect database manually:**

```bash
# Connect to PostgreSQL CLI
docker compose exec postgres psql -U postgres -d sri_template_dev

# List tables
\dt

# Describe table
\d "Users"
\d "Products"

# Query data
SELECT * FROM "Users";

# Exit
\q
```

**Export/Import database:**

```bash
# Export
docker compose exec postgres pg_dump -U postgres sri_template_dev > backup.sql

# Import
docker compose exec -T postgres psql -U postgres sri_template_dev < backup.sql
```

---

### Testing Troubleshooting

**Frontend tests fail:**

```bash
# Clear Jest cache
cd frontend
npm test -- --clearCache
npm test
```

**Backend tests fail with "database locked":**

- Each test uses unique in-memory database
- Check for async issues or missing `await`

---

### Network Troubleshooting

**Services can't communicate:**

```bash
# Check Docker network
docker network ls
docker network inspect sri-template_default

# Verify services are on same network
docker compose ps
```

**CORS errors in browser:**

- Check backend `Program.cs` has CORS policy for `http://localhost:3000`
- Verify frontend `.env` has `VITE_API_URL=http://localhost:5002`

---

### Performance Issues

**Slow Docker builds:**

```bash
# Use BuildKit for faster builds
export DOCKER_BUILDKIT=1
docker compose build
```

**High CPU usage:**

- Backend `dotnet watch` rebuilds on every change
- Consider using local development for intensive work

**Out of memory:**

```bash
# Increase Docker Desktop memory allocation
# Docker Desktop → Settings → Resources → Memory

# Or use local development (uses less RAM)
```

---

### Getting Help

If you're still stuck after trying these solutions:

1. **Check logs:** `docker compose logs -f`
2. **Verify all prerequisites** are installed
3. **Try clean rebuild:** `docker compose down -v && docker compose up --build`
4. **Check GitHub issues** for similar problems
5. **Create detailed issue** with:
   - Error message
   - Operating system
   - Docker version (`docker --version`)
   - Node version (`node --version`)
   - .NET version (`dotnet --version`)
   - Steps to reproduce





