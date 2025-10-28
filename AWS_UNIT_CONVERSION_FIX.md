# Unit Conversion Service Fix - Deployment Guide

## Issue Summary

The production deployment was experiencing 500 Internal Server Error when creating vessels due to a missing unit conversion configuration file (`unit-systems.xml`).

### Error Symptoms

1. **500 Error on Vessel Creation**: POST to `/api/v1/hydrostatics/vessels` returned 500 error
2. **Settings Load Error**: Frontend displayed "Failed to load settings: rt"

### Root Cause

The unit conversion service was looking for `config/unit-systems.xml` but:

1. The Program.cs was passing an incorrect path to the UnitConverter constructor
2. The Docker build context in the deployment script was incorrect

## Fixes Applied

### 1. Fixed Unit Converter Path (Backend Services)

**Files Changed:**

- `backend/DataService/Program.cs`
- `backend/ApiGateway/Program.cs`

**Change:** Pass `null` to UnitConverter constructor to use the default path (`config/unit-systems.xml`)

```csharp
// Before
var xmlPath = Path.Combine(AppContext.BaseDirectory, "unit-systems.xml");
builder.Services.AddSingleton<NavArch.UnitConversion.Services.IUnitConverter>(sp =>
    new NavArch.UnitConversion.Services.UnitConverter(xmlPath));

// After
builder.Services.AddSingleton<NavArch.UnitConversion.Services.IUnitConverter>(sp =>
    new NavArch.UnitConversion.Services.UnitConverter(null));
```

### 2. Ensured Config File is Copied to Output

**Files Changed:**

- `backend/DataService/DataService.csproj`
- `backend/ApiGateway/ApiGateway.csproj`

**Added:**

```xml
<ItemGroup>
  <None Include="..\..\packages\unit-conversion\config\unit-systems.xml">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Link>config\unit-systems.xml</Link>
  </None>
</ItemGroup>
```

### 3. Fixed Docker Build Context

**File Changed:** `scripts/build-and-push.ps1`

**Change:** Use repository root (`.`) as build context instead of `backend/`

```powershell
# Before
docker build -t $DATA_SERVICE_REPO:latest -f backend/DataService/Dockerfile backend/

# After
docker build -t $DATA_SERVICE_REPO:latest -f backend/DataService/Dockerfile .
```

### 4. Improved Error Handling

**File Changed:** `backend/DataService/Controllers/VesselsController.cs`

**Added:** Catch-all exception handler to log and return meaningful error messages:

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error creating vessel");
    return StatusCode(StatusCodes.Status500InternalServerError,
        new { error = "An unexpected error occurred while creating the vessel", details = ex.Message });
}
```

## Verification

### Local Build Verification

✅ Backend builds successfully with `dotnet build`
✅ Config file present at `backend/DataService/bin/Debug/net8.0/config/unit-systems.xml`
✅ Config file present at `backend/ApiGateway/bin/Debug/net8.0/config/unit-systems.xml`
✅ Docker build succeeds with corrected context
✅ Config file present in Docker image at `/app/config/unit-systems.xml`

## Deployment Steps

### Option 1: Deploy to AWS (Recommended)

1. **Build and push new Docker images:**

   ```powershell
   cd C:\Abhi\Projects\Sri\navarch-studio
   .\scripts\build-and-push.ps1 -Environment dev
   ```

2. **Update App Runner services:**

   ```powershell
   .\scripts\deploy.ps1 -Environment dev
   ```

3. **Verify deployment:**
   - Check App Runner service logs in AWS Console
   - Test vessel creation via frontend
   - Verify settings load correctly

### Option 2: Test Locally First

1. **Clean and rebuild:**

   ```powershell
   cd backend
   dotnet clean
   dotnet build
   ```

2. **Start services with Docker Compose:**

   ```powershell
   cd ..
   docker-compose up --build
   ```

3. **Test endpoints:**
   - GET `http://localhost:5002/api/v1/users/settings` - Should return default settings
   - POST `http://localhost:5002/api/v1/hydrostatics/vessels` - Should create vessel

## What Changed in Production

After deployment, the following will be fixed:

1. ✅ Unit conversion service will load configuration correctly
2. ✅ Vessel creation will work properly
3. ✅ Settings endpoint will return proper JSON response
4. ✅ Error messages will be more descriptive if issues occur

## Monitoring After Deployment

Check the following in AWS CloudWatch Logs:

1. Look for: `"Unit conversion service registered with default config path"` in startup logs
2. Verify no file-not-found errors related to `unit-systems.xml`
3. Test vessel creation through the frontend UI
4. Check that settings load without errors

## Rollback Plan

If issues occur after deployment:

1. Check App Runner service logs in CloudWatch
2. Verify the Docker image has the config file:
   ```bash
   docker run --rm --entrypoint ls <image>:latest -la config/
   ```
3. If needed, revert to previous image by manually updating App Runner service image identifier

## Additional Notes

- The fix does not require database migrations
- No changes to frontend code were needed
- The fix is backward compatible with existing data
- Frontend will automatically retry settings load if needed

## Support

If you encounter issues after deployment:

1. Check CloudWatch Logs for detailed error messages
2. Verify environment variables in App Runner service configuration
3. Ensure RDS database is accessible from App Runner services
4. Check that all three services (Identity, API Gateway, Data) are healthy
