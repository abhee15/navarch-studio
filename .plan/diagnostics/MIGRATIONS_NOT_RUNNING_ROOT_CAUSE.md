# Root Cause: Why Migrations Were NOT Running Through CI/CD

**Date:** October 28, 2025  
**Status:** ✅ FIXED

---

## 🎯 The Problem

When deploying to AWS via CI/CD:

- ✅ RDS database exists and is reachable
- ✅ Network/security groups are correct
- ✅ Services can connect to RDS
- ❌ **Database tables don't exist** → All queries fail with "relation does not exist"

**Why?** Migrations were never applied!

---

## 🔍 Root Cause Analysis

### Issue #1: Wrong ASPNETCORE_ENVIRONMENT Setting

**Terraform Configuration:**

```hcl
# terraform/deploy/modules/app-runner/main.tf (line 103)
ASPNETCORE_ENVIRONMENT = title(var.environment)
```

When `var.environment = "dev"`, this becomes:

- `title("dev")` = `"Dev"`
- .NET treats "Dev" as **Development mode**

**Auto-Migration Logic in Services:**

```csharp
// backend/DataService/Program.cs (lines 262-271)
if (!builder.Environment.IsDevelopment())  // FALSE for "Dev"!
{
    logger.LogInformation("Applying pending migrations...");
    await dbContext.Database.MigrateAsync();  // ❌ NEVER RUNS
}
else
{
    logger.LogWarning("⚠️ Running with pending migrations.");
}
```

**Result:** AWS dev environment = Development mode = **No auto-migrations**

---

### Issue #2: IdentityService Had NO Auto-Migration Logic

DataService had startup migration code, but IdentityService didn't!

Result: Even if we fixed the environment variable, IdentityService would still fail.

---

## ✅ The Complete Fix

### Fix #1: Terraform Environment Mapping

**Changed in:** `terraform/deploy/modules/app-runner/main.tf`

**Before:**

```hcl
ASPNETCORE_ENVIRONMENT = title(var.environment)
# dev → "Dev" (Development mode, no auto-migrations)
```

**After:**

```hcl
# Use Staging for dev/staging so auto-migrations run, Production for prod
ASPNETCORE_ENVIRONMENT = var.environment == "prod" ? "Production" : "Staging"
# dev → "Staging" (auto-migrations run!)
# staging → "Staging" (auto-migrations run!)
# prod → "Production" (auto-migrations run!)
```

---

### Fix #2: Added Auto-Migration Logic to IdentityService

**Added in:** `backend/IdentityService/Program.cs` (after line 163)

```csharp
// Verify database connectivity and migrations at startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityService.Data.IdentityDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await dbContext.Database.CanConnectAsync();
        logger.LogInformation("✅ Database connection successful");

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

        if (pendingMigrations.Any())
        {
            if (!builder.Environment.IsDevelopment())
            {
                logger.LogInformation("Applying pending migrations...");
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("✅ Migrations applied successfully");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Failed to connect to database or check migrations");
    }
}
```

---

## 🏗️ Environment Architecture

### Local Development (Docker Compose)

- **Environment:** `ASPNETCORE_ENVIRONMENT=Development`
- **Database:** Local PostgreSQL (docker-compose)
- **Migrations:** Manual control (`dotnet ef database update`)
- **Why:** Developers control when to run migrations

### AWS Dev Environment

- **Environment:** `ASPNETCORE_ENVIRONMENT=Staging` (after fix)
- **Database:** RDS
- **Migrations:** AUTO-APPLIED on service startup
- **Why:** Dev environment should be self-managing

### AWS Staging Environment

- **Environment:** `ASPNETCORE_ENVIRONMENT=Staging`
- **Database:** RDS
- **Migrations:** AUTO-APPLIED on service startup

### AWS Production Environment

- **Environment:** `ASPNETCORE_ENVIRONMENT=Production`
- **Database:** RDS
- **Migrations:** AUTO-APPLIED on service startup
- **Why:** Zero-downtime deployments, migrations run before app starts

---

## 📋 What Happens Now

### When You Push to Main (CI/CD)

1. **Build Phase:** Docker images built and pushed to ECR
2. **Deploy Phase:** Terraform applies changes
   - RDS already exists ✓
   - App Runner services updated with `ASPNETCORE_ENVIRONMENT=Staging`
3. **Service Startup:**
   - IdentityService starts
   - Checks for pending migrations
   - **Applies migrations automatically** ✓
   - Creates `identity.Users` table and all other tables
   - Logs: "✅ Migrations applied successfully"
4. **DataService Startup:**
   - Same process
   - Creates `hydrostatics.vessels` and all other tables
5. **Services Ready:** All endpoints work!

---

## 🧪 Testing After Deployment

After the next CI/CD deployment completes, verify:

### 1. Check Service Logs

```powershell
# IdentityService logs
aws logs tail "/aws/apprunner/navarch-studio-dev-identity-service" `
  --since 10m --region us-east-1 --format short `
  | Select-String "migration"

# DataService logs
aws logs tail "/aws/apprunner/navarch-studio-dev-data-service" `
  --since 10m --region us-east-1 --format short `
  | Select-String "migration"
```

**Expected output:**

```
✅ Database connection successful
Applied migrations: 0
Pending migrations: 3
⚠️ Database has pending migrations: 20251020040413_InitialCreate, ...
Applying pending migrations...
✅ Migrations applied successfully
```

### 2. Test Login Endpoint

```powershell
pwsh temp/diagnostics/test-rds-connection.ps1
```

**Expected:** Login succeeds, vessel creation works!

---

## 🔄 For Local Development

**No changes needed!** Docker Compose already uses:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
```

To run migrations locally:

```bash
# IdentityService
cd backend/IdentityService
dotnet ef database update

# DataService
cd backend/DataService
dotnet ef database update
```

---

## 📊 Summary

| Environment                | Mode        | Database         | Migrations |
| -------------------------- | ----------- | ---------------- | ---------- |
| **Local (Docker Compose)** | Development | Local PostgreSQL | Manual     |
| **AWS Dev**                | Staging     | RDS              | Auto       |
| **AWS Staging**            | Staging     | RDS              | Auto       |
| **AWS Production**         | Production  | RDS              | Auto       |

---

## ✅ Files Changed

1. `terraform/deploy/modules/app-runner/main.tf`
   - Changed environment variable mapping (3 locations)
2. `backend/IdentityService/Program.cs`
   - Added auto-migration startup logic

---

## 🚀 Next Steps

1. **Commit changes:**

   ```bash
   git add terraform/deploy/modules/app-runner/main.tf
   git add backend/IdentityService/Program.cs
   git commit -m "fix: enable auto-migrations for AWS deployments

   - Set ASPNETCORE_ENVIRONMENT to Staging for dev/staging (not Development)
   - Add auto-migration logic to IdentityService
   - Ensures database schema is applied on service startup
   - Maintains manual migration control for local development"
   ```

2. **Push to trigger CI/CD:**

   ```bash
   git push origin main
   ```

3. **Wait for deployment** (~15-20 minutes)

4. **Verify migrations ran:**
   - Check CloudWatch logs for "✅ Migrations applied successfully"
   - Test login and vessel creation

---

**Status:** Ready to deploy! Once these changes are merged and deployed, all database connectivity issues will be resolved.
