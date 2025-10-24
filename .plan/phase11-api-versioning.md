# API Versioning Strategy

**Status**: Ready to Implement  
**Priority**: MEDIUM 🟡  
**Estimated Time**: 30-45 minutes  
**Part of**: Phase 11 - Security Hardening

---

## Overview

Implement a robust API versioning strategy to:

- Maintain backward compatibility
- Enable gradual migration for clients
- Provide clear API contracts
- Support multiple versions simultaneously

---

## Versioning Approach: URL Path Versioning

**Chosen Strategy**: URL path-based versioning (`/api/v1/`, `/api/v2/`)

**Why URL Path Versioning?**

- ✅ Most explicit and clear
- ✅ Easy to test and debug (use in browser, Postman)
- ✅ Industry standard (used by Stripe, GitHub, Twitter)
- ✅ Works perfectly with API Gateway pattern
- ✅ No ambiguity in routing

**Example URLs:**

```
/api/v1/auth/login
/api/v1/users
/api/v1/products

/api/v2/users      # New version with breaking changes
/api/v2/products   # Updated product schema
```

---

## Current State

**Current Routes** (all services):

```csharp
[Route("api/v1/[controller]")]  // ✅ Already using v1!
public class UsersController : ControllerBase
{
    // ...
}
```

**Good News**: We're already using `v1` in routes! We just need to:

1. Formalize the versioning strategy
2. Add version negotiation middleware
3. Set up deprecation headers
4. Document version lifecycle

---

## Implementation Plan

### Step 1: Install ASP.NET API Versioning Package

**Add to all services** (IdentityService, ApiGateway, DataService):

```bash
cd backend/IdentityService
dotnet add package Asp.Versioning.Mvc --version 8.0.0

cd ../ApiGateway
dotnet add package Asp.Versioning.Mvc --version 8.0.0

cd ../DataService
dotnet add package Asp.Versioning.Mvc --version 8.0.0
```

---

### Step 2: Configure API Versioning in Program.cs

**Add to each service's `Program.cs`** (after `builder.Services.AddControllers()`):

```csharp
// API Versioning
builder.Services.AddApiVersioning(options =>
{
    // Report API versions in response headers
    options.ReportApiVersions = true;

    // Default version if client doesn't specify
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);

    // Read version from URL path (e.g., /api/v1/users)
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc() // Add MVC API explorer for versioning
.AddApiExplorer(options =>
{
    // Format version as "'v'major[.minor][-status]" (e.g., v1.0, v2.0-beta)
    options.GroupNameFormat = "'v'VVV";

    // Substitute version in route template
    options.SubstituteApiVersionInUrl = true;
});
```

---

### Step 3: Update Controller Attributes

**Current** (already good!):

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    // ...
}
```

**Update to use version parameter**:

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
    // All methods inherit v1.0
}
```

**For future v2 controllers**:

```csharp
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersV2Controller : ControllerBase
{
    // v2.0 methods with breaking changes
}
```

---

### Step 4: Add Version Deprecation Support

**Example: Deprecate v1, encourage v2**:

```csharp
[ApiController]
[ApiVersion("1.0", Deprecated = true)]  // Mark as deprecated
[ApiVersion("2.0")]                      // Current version
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
    // Supports both v1 (deprecated) and v2
}
```

**Add deprecation middleware** (in `Program.cs`):

```csharp
// Add custom middleware to include deprecation warnings
app.Use(async (context, next) =>
{
    await next();

    // If deprecated version was used, add warning header
    var apiVersionFeature = context.Features.Get<IApiVersioningFeature>();
    if (apiVersionFeature?.RequestedApiVersion != null)
    {
        var version = apiVersionFeature.RequestedApiVersion;
        var model = context.GetEndpoint()?.Metadata.GetMetadata<ApiVersionModel>();

        if (model?.DeprecatedApiVersions.Contains(version) == true)
        {
            context.Response.Headers.Add("X-API-Deprecated", "true");
            context.Response.Headers.Add("X-API-Deprecated-Version", version.ToString());
            context.Response.Headers.Add("X-API-Sunset-Date", "2025-12-31"); // Example sunset date
            context.Response.Headers.Add("Link", "</api/v2/>; rel=\"successor-version\"");
        }
    }
});
```

---

### Step 5: Update Swagger for Versioning

**Modify Swagger configuration** to support multiple versions:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    // Define a Swagger document for each version
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Identity Service API",
        Version = "v1",
        Description = "Version 1 of the Identity Service API"
    });

    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Identity Service API",
        Version = "v2",
        Description = "Version 2 of the Identity Service API (Future)"
    });

    // Use API versioning to generate separate docs
    options.OperationFilter<SwaggerDefaultValues>();
});

// Enable Swagger UI for multiple versions
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
    options.SwaggerEndpoint("/swagger/v2/swagger.json", "API V2 (Future)");
});
```

---

### Step 6: Frontend Integration

**Update frontend API client** to explicitly use v1:

```typescript
// frontend/src/services/api.ts
const API_VERSION = "v1";

export const client = axios.create({
  baseURL: `${import.meta.env.VITE_API_URL}/api/${API_VERSION}`,
  headers: {
    "Content-Type": "application/json",
  },
});
```

**Add version header for tracking**:

```typescript
client.interceptors.request.use((config) => {
  config.headers["X-Client-Version"] = "1.0.0"; // Track client version
  return config;
});
```

---

## Version Lifecycle

### Version States

1. **Current** - Actively maintained, recommended for new projects
2. **Supported** - Maintained for compatibility, no new features
3. **Deprecated** - Still works but will be removed, sunset date announced
4. **Retired** - No longer available, returns 410 Gone

### Recommended Timeline

```
v1.0 Released              → Current
  ↓ (6 months)
v2.0 Released              → v2: Current, v1: Supported
  ↓ (6 months)
v1.0 Deprecated            → v2: Current, v1: Deprecated (12 months notice)
  ↓ (12 months)
v1.0 Retired               → v2: Current, v1: 410 Gone
```

---

## Breaking Changes Policy

**What constitutes a breaking change?**

✅ **Requires new version:**

- Removing fields from response
- Changing field types
- Removing endpoints
- Changing authentication methods
- Changing error response formats

❌ **Does NOT require new version:**

- Adding new fields to response (backward compatible)
- Adding new endpoints
- Adding new optional parameters
- Bug fixes
- Performance improvements

---

## Response Headers

**All API responses include**:

```
HTTP/1.1 200 OK
api-supported-versions: 1.0, 2.0
api-deprecated-versions: 1.0
X-API-Version: 1.0
X-API-Deprecated: true
X-API-Sunset-Date: 2025-12-31
Link: </api/v2/>; rel="successor-version"
```

---

## Testing Strategy

### Test Multiple Versions

```csharp
// IdentityService.Tests/VersioningTests.cs
public class VersioningTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task V1_Endpoint_Returns_Success()
    {
        var response = await _client.GetAsync("/api/v1/users/123");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task V2_Endpoint_Returns_Success()
    {
        var response = await _client.GetAsync("/api/v2/users/123");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Deprecated_Version_Returns_Warning_Header()
    {
        var response = await _client.GetAsync("/api/v1/users/123");
        Assert.True(response.Headers.Contains("X-API-Deprecated"));
    }
}
```

---

## Documentation Updates

### Update README.md

````markdown
## API Versioning

This API uses URL path versioning. All endpoints are prefixed with `/api/v{version}/`.

**Current Version**: v1  
**Supported Versions**: v1  
**Deprecated Versions**: None

**Example Requests**:

```bash
# Current version
curl https://api.example.com/api/v1/users

# Explicit version
curl https://api.example.com/api/v2/users
```
````

**Version Lifecycle**: See [API_VERSIONING.md](docs/API_VERSIONING.md) for details.

````

---

## Migration Example: v1 → v2

**Scenario**: Change user response format

**v1 Response**:
```json
{
  "id": "123",
  "email": "user@example.com",
  "name": "John Doe",
  "createdAt": "2024-01-01T00:00:00Z"
}
````

**v2 Response** (breaking change - nested structure):

```json
{
  "id": "123",
  "profile": {
    "email": "user@example.com",
    "fullName": "John Doe"
  },
  "metadata": {
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-06-01T00:00:00Z"
  }
}
```

**Implementation**:

```csharp
// V1 Controller (keeps existing format)
[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<UserDto> GetUser(string id)
    {
        // Return flat structure (v1 format)
        return new UserDto { /* ... */ };
    }
}

// V2 Controller (new format)
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<UserV2Dto> GetUser(string id)
    {
        // Return nested structure (v2 format)
        return new UserV2Dto { /* ... */ };
    }
}
```

---

## Benefits

✅ **Backward Compatibility** - Existing clients keep working  
✅ **Gradual Migration** - Clients upgrade at their own pace  
✅ **Clear Contracts** - Version in URL makes API contract explicit  
✅ **Easy Testing** - Test multiple versions independently  
✅ **Better Monitoring** - Track version usage in logs  
✅ **Professional API** - Industry-standard versioning

---

## Next Steps

1. Install `Asp.Versioning.Mvc` package in all services
2. Update `Program.cs` with versioning configuration
3. Update controller route attributes
4. Add deprecation middleware
5. Update Swagger configuration
6. Update frontend API client
7. Add versioning tests
8. Document in README

**Estimated Time**: 30-45 minutes

---

## References

- [Microsoft API Versioning Docs](https://github.com/dotnet/aspnet-api-versioning)
- [REST API Versioning Best Practices](https://www.freecodecamp.org/news/rest-api-best-practices-rest-endpoint-design-examples/)
- [Stripe API Versioning](https://stripe.com/docs/api/versioning)
- [GitHub API Versioning](https://docs.github.com/en/rest/overview/api-versions)





