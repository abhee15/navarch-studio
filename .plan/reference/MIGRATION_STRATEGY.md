# Database Migration Strategy

## Overview

This project uses **service-based auto-migrations** for database schema management. Migrations are automatically applied by the services themselves when they start up in non-Development environments (Staging/Production).

## Why This Approach?

- **No NAT Gateway Required**: Running migrations from GitHub Actions would require a NAT Gateway ($32+/month) to reach private RDS
- **No Public RDS**: Making RDS publicly accessible is a security risk
- **Simple & Cost-Effective**: Services already have VPC access to RDS
- **Reliable**: Services won't start properly without correct schema, providing immediate feedback

## How It Works

### 1. Service Startup Flow

When a service (IdentityService or DataService) starts:

1. ✅ Build the application (`builder.Build()`)
2. ✅ **Start service immediately** (respond to health checks)
3. 🔄 **Background Task**: Check database connectivity (2-second delay)
4. 📊 **Background Task**: Query pending migrations
5. **If in Development**: Log warning, require manual migration
6. **If in Staging/Production**: Automatically apply pending migrations in background
7. ✅ Service remains healthy throughout migration

**Why Background Task?**
- App Runner health checks timeout after 5 seconds
- Database migrations can take 10-30 seconds
- Running migrations synchronously causes health check failures → restart loop
- Background migrations allow service to respond to health checks immediately

### 2. Environment Behavior

| Environment | Auto-Migration | Manual Required |
|-------------|----------------|-----------------|
| Development (Local) | ❌ No | ✅ Yes (`dotnet ef database update`) |
| Staging (AWS dev) | ✅ Yes | ❌ No |
| Production (AWS prod) | ✅ Yes | ❌ No |

## Naming Convention

Both services use **snake_case** naming convention for PostgreSQL:
- ✅ IdentityService: `users`, `roles`, `user_roles`
- ✅ DataService: `vessels`, `stations`, `waterlines`, `offsets`

This is PostgreSQL best practice and prevents naming conflicts.

## Fresh Environment Deployment

When you deploy to a fresh environment (no existing database):

1. **Terraform creates RDS instance** (empty database)
2. **Services deploy to App Runner**
3. **Services start and detect pending migrations**
4. **Services automatically create all tables** (via auto-migration)
5. ✅ **Application is ready to use**

**No manual intervention required!**

## Destroying & Redeploying

To test the complete cycle:

```powershell
# 1. Destroy environment
gh workflow run destroy-dev.yml

# 2. Wait for destruction to complete (~5 minutes)
gh run list --workflow=destroy-dev.yml --limit 1

# 3. Deploy fresh environment
git push

# 4. Verify migrations ran automatically
# Check CloudWatch logs for:
# - "🔄 Starting database migration check..."
# - "🔄 Auto-applying X pending migrations..."
# - "✅ Migrations applied successfully!"
```

## Monitoring Migration Status

### CloudWatch Logs

Check service logs in AWS Console or CLI:

```powershell
# DataService migration logs
aws logs tail /aws/apprunner/navarch-studio-dev-data-service/.../application --region us-east-1 --since 10m --filter-pattern "migration"

# IdentityService migration logs
aws logs tail /aws/apprunner/navarch-studio-dev-identity-service/.../application --region us-east-1 --since 10m --filter-pattern "migration"
```

### Log Messages to Look For

✅ **Success**:
```
🔄 Starting database migration check...
✅ Database connection successful: True
📊 Migration status - Applied: X, Pending: Y
🔄 Auto-applying Y pending migrations in Staging environment...
✅ Migrations applied successfully!
✅ Database migration check complete
```

❌ **Failure**:
```
❌ Migration check failed: <error message>
```

## Troubleshooting

### Service Won't Start

If a service fails to start after deployment:

1. **Check CloudWatch logs** for migration errors
2. **Verify RDS is accessible** from App Runner (security groups)
3. **Check database credentials** in Secrets Manager

### Schema Conflicts

If you see errors about existing tables:

1. **Ensure both services use snake_case** (already configured)
2. **Check migration history**: Both services use the default `__EFMigrationsHistory` table
3. **Each service manages its own tables** - no conflicts

### Manual Migration (Emergency Only)

If auto-migrations fail and you need to run manually:

1. **Temporarily make RDS public** (security risk!)
2. **Run migrations from local machine**:
   ```powershell
   pwsh -ExecutionPolicy Bypass .\temp\make-rds-public.ps1
   pwsh -ExecutionPolicy Bypass .\temp\run-migrations-with-wait.ps1
   pwsh -ExecutionPolicy Bypass .\temp\revert-rds-private.ps1
   ```
3. **Redeploy services** to ensure they start properly

## Code Locations

### Auto-Migration Logic

- **IdentityService**: `backend/IdentityService/Program.cs` (lines 173-221)
- **DataService**: `backend/DataService/Program.cs` (lines 237-285)

### Database Configuration

- **IdentityService**: `backend/IdentityService/Program.cs` (lines 68-77)
  - Uses `UseSnakeCaseNamingConvention()`
  - Connection string: `ConnectionStrings__DefaultConnection`

- **DataService**: `backend/DataService/Program.cs` (lines 75-96)
  - Uses `UseSnakeCaseNamingConvention()`
  - Connection string: `ConnectionStrings__DefaultConnection`

### Terraform

- **RDS Instance**: `terraform/deploy/rds.tf`
- **App Runner Services**: `terraform/deploy/modules/app-runner/main.tf`
- **Environment Variables**: Set `ASPNETCORE_ENVIRONMENT=Staging` for dev/staging

## Best Practices

1. ✅ **Always test migrations locally first**: Run `dotnet ef database update` before pushing
2. ✅ **Check service logs after deployment**: Verify migrations ran successfully
3. ✅ **Use descriptive migration names**: `dotnet ef migrations add AddVesselStationsTable`
4. ✅ **Never modify applied migrations**: Create new migrations for schema changes
5. ✅ **Keep migrations small**: One logical change per migration

## Future Improvements

- **Health Check Enhancement**: Add database schema version to health endpoint
- **Migration Timeout**: Add configurable timeout for long-running migrations
- **Rollback Strategy**: Document rollback procedures for failed deployments
- **Migration Monitoring**: Alert on migration failures via CloudWatch Alarms

