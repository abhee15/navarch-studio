# Phase 10: CloudWatch Logging

## 🎯 Goal

Implement structured logging with AWS CloudWatch to gain visibility into production issues. Learn why logging matters and how to debug production problems effectively.

## 🤔 The Problem (WHY We Need Logging)

### Current State: Flying Blind

Right now, when something goes wrong in production:

**Scenario 1: User Reports Error**

- User: "I got an error when trying to add a product"
- You: 🤷 No logs, no details, can't reproduce
- Result: User frustrated, bug unfixed

**Scenario 2: Service is Slow**

- Users complaining app is slow
- You: 🤷 Which service? Which endpoint? What's the bottleneck?
- Result: Can't optimize what you can't measure

**Scenario 3: Deployment Breaks Something**

- Deploy at 3pm, users report issues at 5pm
- You: 🤷 What changed? What's the error? Where's it happening?
- Result: 2+ hours to debug (should be 2 minutes with logs)

### What We're Missing

❌ **No visibility** - Can't see what code is doing  
❌ **No context** - Don't know user ID, request ID, or flow  
❌ **No history** - Can't see what happened before error  
❌ **No search** - Can't filter by user, error type, time  
❌ **No alerts** - Discover issues hours/days later

## 💡 The Solution (WHAT We're Building)

### Structured Logging with CloudWatch

**Structured = JSON logs with consistent fields**

Bad (unstructured):

```
User logged in successfully
Error occurred
Database connection failed
```

Good (structured):

```json
{
  "timestamp": "2024-10-23T10:30:00Z",
  "level": "Info",
  "message": "User logged in successfully",
  "userId": "user-123",
  "requestId": "req-abc-456",
  "service": "IdentityService"
}
```

**Why structured?** You can search, filter, and analyze easily!

### Three-Layer Implementation

```
┌─────────────────────────────────────────────────┐
│          Frontend (React)                       │
│  ├─ Console logs (dev)                          │
│  └─ Error logs → Backend → CloudWatch (prod)    │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│      Backend (.NET Services)                    │
│  ├─ Serilog (structured logging library)        │
│  ├─ AWS.Logger.SerilogSink (CloudWatch sink)    │
│  └─ Correlation IDs (track requests)            │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│         AWS CloudWatch Logs                     │
│  ├─ Log Groups: /aws/apprunner/service-name     │
│  ├─ Retention: 7 days (dev), 30 days (prod)     │
│  ├─ CloudWatch Insights (search & analyze)      │
│  └─ Free: 5GB/month ingestion                   │
└─────────────────────────────────────────────────┘
```

## 📚 Learning Objectives

By the end of this phase, you'll understand:

1. **Why structured logging matters** (vs unstructured)
2. **How to implement Serilog** in .NET services
3. **What correlation IDs are** and why they're critical
4. **How to configure CloudWatch** log groups and retention
5. **How to search logs** with CloudWatch Insights
6. **Log levels** (Debug, Info, Warning, Error) and when to use each
7. **Cost management** (stay within 5GB free tier)

## 🛠️ Implementation Steps

### Step 1: Install Serilog Packages (Backend)

**What**: Add Serilog NuGet packages to all three services

**Why**: Serilog is the industry standard for .NET logging with great CloudWatch support

**Services to Update**:

- `backend/IdentityService/IdentityService.csproj`
- `backend/ApiGateway/ApiGateway.csproj`
- `backend/DataService/DataService.csproj`

**Packages**:

```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="AWS.Logger.SerilogSink" Version="3.5.0" />
<PackageReference Include="Serilog.Enrichers.Environment" Version="2.3.0" />
<PackageReference Include="Serilog.Enrichers.Process" Version="2.0.2" />
<PackageReference Include="Serilog.Enrichers.Thread" Version="3.1.0" />
```

**Command** (run in each service folder):

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package AWS.Logger.SerilogSink
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Enrichers.Process
dotnet add package Serilog.Enrichers.Thread
```

### Step 2: Configure Serilog in Program.cs

**What**: Replace default .NET logging with Serilog

**File**: Each service's `Program.cs`

**Before** (default logging):

```csharp
var builder = WebApplication.CreateBuilder(args);
```

**After** (Serilog):

```csharp
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("ServiceName", "IdentityService") // Change per service
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.AWSSerilog(
        configuration: builder.Configuration,
        textFormatter: new CompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();
```

**Why each part matters**:

- `ReadFrom.Configuration` - Gets settings from appsettings.json
- `Enrich.FromLogContext` - Adds contextual properties (user ID, request ID)
- `Enrich.WithMachineName` - Adds server name (useful for multiple instances)
- `Enrich.WithEnvironmentName` - Adds Dev/Staging/Prod
- `WriteTo.Console` - Logs to console (for local dev)
- `WriteTo.AWSSerilog` - Logs to CloudWatch (production)
- `CompactJsonFormatter` - Structured JSON format

### Step 3: Add Serilog Configuration to appsettings.json

**What**: Configure log levels and CloudWatch settings

**File**: Each service's `appsettings.json` and `appsettings.Development.json`

**appsettings.json** (Production):

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "AWSSerilog",
        "Args": {
          "logGroup": "/aws/apprunner/sri-template-identity-service",
          "region": "us-east-1"
        }
      }
    ]
  }
}
```

**appsettings.Development.json** (Local):

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "System": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

**Log Levels Explained**:

- `Debug` - Detailed info for debugging (dev only)
- `Information` - General flow (user logged in, request started)
- `Warning` - Unexpected but handled (retry failed, slow query)
- `Error` - Errors that need attention (database connection failed)
- `Fatal` - App-crashing errors (out of memory)

### Step 4: Add Correlation ID Middleware

**What**: Track requests across services with unique IDs

**Why**: When a request hits API Gateway → Identity Service → Database, you need to connect all logs for that request

**File**: `backend/Shared/Middleware/CorrelationIdMiddleware.cs`

```csharp
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Shared.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
```

**Register middleware** in each service's `Program.cs`:

```csharp
app.UseMiddleware<CorrelationIdMiddleware>();
```

### Step 5: Add Structured Logging to Controllers

**What**: Log important actions with context

**Example**: `IdentityService/Controllers/AuthController.cs`

**Before**:

```csharp
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var result = await _userService.LoginAsync(request.Email, request.Password);
    return Ok(result);
}
```

**After**:

```csharp
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    _logger.LogInformation(
        "Login attempt for user {Email}",
        request.Email);

    try
    {
        var result = await _userService.LoginAsync(request.Email, request.Password);

        _logger.LogInformation(
            "User {Email} logged in successfully",
            request.Email);

        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex,
            "Login failed for user {Email}: {ErrorMessage}",
            request.Email,
            ex.Message);

        return Unauthorized();
    }
}
```

**What to log**:

- ✅ User actions (login, signup, logout, create, update, delete)
- ✅ API calls to other services
- ✅ Database queries (slow ones)
- ✅ Errors and exceptions
- ❌ Don't log passwords, tokens, or sensitive data!

### Step 6: Configure CloudWatch Log Groups

**What**: Set up log groups in AWS with proper retention

**Where**: `terraform/setup/cloudwatch.tf` (already exists, may need updates)

**Add retention policies**:

```hcl
resource "aws_cloudwatch_log_group" "identity_service" {
  name              = "/aws/apprunner/${var.project_name}-identity-service"
  retention_in_days = var.environment == "dev" ? 7 : 30

  tags = merge(local.common_tags, {
    Service = "IdentityService"
  })
}

resource "aws_cloudwatch_log_group" "api_gateway" {
  name              = "/aws/apprunner/${var.project_name}-api-gateway"
  retention_in_days = var.environment == "dev" ? 7 : 30

  tags = merge(local.common_tags, {
    Service = "ApiGateway"
  })
}

resource "aws_cloudwatch_log_group" "data_service" {
  name              = "/aws/apprunner/${var.project_name}-data-service"
  retention_in_days = var.environment == "dev" ? 7 : 30

  tags = merge(local.common_tags, {
    Service = "DataService"
  })
}
```

**Retention Strategy**:

- Dev: 7 days (minimize storage costs)
- Staging: 14 days
- Production: 30 days (or more for compliance)

### Step 7: Frontend Error Logging

**What**: Send frontend errors to backend for logging

**File**: `frontend/src/services/api.ts`

**Add response interceptor**:

```typescript
// Response interceptor - Log errors
client.interceptors.response.use(
  (response) => response,
  async (error) => {
    // Log error to backend
    const errorLog = {
      message: error.message,
      url: error.config?.url,
      method: error.config?.method,
      status: error.response?.status,
      timestamp: new Date().toISOString(),
      userAgent: navigator.userAgent,
    };

    // Send to backend logging endpoint (don't await to avoid blocking)
    try {
      await client.post("/api/v1/logs/frontend-error", errorLog);
    } catch {
      // Silently fail - don't block user if logging fails
      console.error("Failed to log error to backend");
    }

    // Handle 401 errors
    if (error.response?.status === 401) {
      const cognitoUser = userPool.getCurrentUser();
      if (cognitoUser) {
        cognitoUser.signOut();
      }
      window.location.href = "/login";
    }

    return Promise.reject(error);
  }
);
```

**Create logging endpoint** in API Gateway:

```csharp
[HttpPost("frontend-error")]
public IActionResult LogFrontendError([FromBody] FrontendErrorLog log)
{
    _logger.LogError(
        "Frontend error: {Message} at {Url} - User Agent: {UserAgent}",
        log.Message,
        log.Url,
        log.UserAgent);

    return Ok();
}
```

## 🧪 Testing & Verification

### Local Testing

1. **Run services locally**:

```bash
cd backend/IdentityService
dotnet run
```

2. **Check console logs** - Should see JSON formatted logs:

```json
{
  "@t": "2024-10-23T10:30:00.123Z",
  "@l": "Information",
  "@m": "User user@example.com logged in successfully",
  "CorrelationId": "abc-123-def",
  "ServiceName": "IdentityService",
  "MachineName": "DESKTOP-ABC"
}
```

3. **Make API requests** - Logs should include correlation IDs

### CloudWatch Testing (After Deployment)

1. **Go to CloudWatch Console**:

   - AWS Console → CloudWatch → Log Groups
   - Find: `/aws/apprunner/sri-template-identity-service`

2. **View logs** - Should see structured JSON logs

3. **Test CloudWatch Insights**:

   ```
   fields @timestamp, @message, CorrelationId, ServiceName
   | filter @message like /error/
   | sort @timestamp desc
   | limit 20
   ```

4. **Search by user**:

   ```
   fields @timestamp, @message, Email
   | filter Email = "user@example.com"
   | sort @timestamp desc
   ```

5. **Find slow requests**:
   ```
   fields @timestamp, @message, Duration
   | filter Duration > 1000
   | sort Duration desc
   ```

## 💰 Cost Management (Stay Free)

### Free Tier Limits

- **Ingestion**: 5GB/month
- **Storage**: First 5GB free
- **Insights queries**: 5GB scanned data/month

### How to Stay Under 5GB

**Typical log sizes**:

- Each log line: ~500 bytes (0.5 KB)
- 10,000 requests/day = ~5 MB/day = 150 MB/month ✅
- 100,000 requests/day = ~50 MB/day = 1.5 GB/month ✅
- 1,000,000 requests/day = ~500 MB/day = 15 GB/month ⚠️ (over limit)

**If you exceed 5GB**:

1. Reduce retention (7 days instead of 30)
2. Increase log level (Warning instead of Information)
3. Filter out noisy logs (health checks)
4. Cost: ~$0.50/GB after free tier

### Log Filtering Strategy

**Filter health checks** (they're noisy):

```csharp
.Filter.ByExcluding(evt =>
    evt.MessageTemplate.Text.Contains("/health"))
```

## 📊 CloudWatch Insights Queries (Cheat Sheet)

### Find Errors

```
fields @timestamp, @message, CorrelationId
| filter @message like /error/i
| sort @timestamp desc
| limit 50
```

### Find Slow Requests (>1 second)

```
fields @timestamp, @message, Duration
| filter Duration > 1000
| stats avg(Duration), max(Duration), count() by bin(5m)
```

### User Activity

```
fields @timestamp, @message, Email
| filter Email = "user@example.com"
| sort @timestamp desc
```

### Error Rate by Service

```
fields @timestamp, ServiceName
| filter @message like /error/i
| stats count() as ErrorCount by ServiceName
```

## ✅ Success Criteria

After completing this phase, you should be able to:

- [ ] See structured JSON logs in CloudWatch
- [ ] Search logs by correlation ID
- [ ] Track a user's journey through services
- [ ] Find errors with full context (stack trace, user, time)
- [ ] Identify slow requests
- [ ] Debug production issues in minutes (not hours)

## 🎓 Key Concepts Learned

1. **Structured Logging** - JSON format makes logs searchable
2. **Correlation IDs** - Track requests across services
3. **Log Levels** - Debug/Info/Warning/Error hierarchy
4. **CloudWatch Insights** - SQL-like queries on logs
5. **Cost Management** - Stay within free tier with retention policies

## 📝 Next Steps

After Phase 10, you'll have:

- ✅ Full visibility into what's happening in production
- ✅ Ability to debug issues quickly
- ✅ Foundation for Phase 11: Error Tracking (Sentry)

**Next**: Phase 11 - Error Tracking & Monitoring

## Estimated Time

- **Implementation**: 4-6 hours
- **Testing**: 1 hour
- **Total**: 5-7 hours (includes learning time)

---

**Ready to implement?** Let's start with Step 1: Installing Serilog packages!





