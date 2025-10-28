# AWS Settings Error Fix - Debugging Guide

## Problem Description

Error occurring in AWS dev environment when clicking Hydrostatics card:

- **Error Message**: `Failed to load settings: rt`
- **HTTP Status**: 500 Internal Server Error
- **Endpoint**: `/api/v1/users/settings`

This error was **not** occurring in local/Docker environments, only in AWS.

## Root Cause Analysis

The error "rt" was a truncated/improperly parsed error message, likely due to:

1. Different response format from AWS App Runner vs local development
2. Poor error handling in the frontend that didn't properly extract error details
3. Axios error objects not being parsed correctly

## Changes Made

### 1. Frontend Error Handling Improvements

#### `frontend/src/stores/SettingsStore.ts`

- ✅ Added comprehensive axios error detection using `axios.isAxiosError()`
- ✅ Added detailed error logging with full error object inspection
- ✅ Improved error message extraction from various error formats
- ✅ **Added graceful fallback** - app now uses default settings (SI units) if API fails
- ✅ Added console logging throughout the load/update flow
- ✅ Fixed TypeScript linting errors (using `unknown` instead of `any` in catch blocks)

#### `frontend/src/services/api.ts`

- ✅ Added detailed request logging (URL, method, headers)
- ✅ Added detailed response logging (status, data)
- ✅ Added detailed error logging in response interceptor
- ✅ Enhanced error information capture

### 2. Graceful Degradation

The app now **fails gracefully** if settings cannot be loaded:

- Uses default settings: `{ preferredUnits: "SI" }`
- Continues normal operation
- Logs warning instead of breaking
- User can still use all features

### 3. Backend Verification

Verified that:

- ✅ `UsersController.GetSettings()` endpoint exists and is configured correctly
- ✅ API versioning is properly configured (`/api/v1/users/settings`)
- ✅ No authentication required for settings endpoint
- ✅ Returns proper JSON response with camelCase properties

## How to Debug in AWS

### Step 1: Check Browser Console Logs

After deploying these changes, open your AWS dev frontend and check the console for:

```
[SettingsStore] Loading settings from API...
[API] Making GET request to: <BASE_URL>/users/settings
[API] Request config: { ... }
```

Look for the detailed error logs:

```
[SettingsStore] Failed to load settings - Full error: { ... }
[SettingsStore] Axios error details: { ... }
[API] Request failed: { ... }
```

### Step 2: Check for Specific Error Patterns

#### CORS Error

```
Access to XMLHttpRequest at '...' from origin '...' has been blocked by CORS policy
```

**Fix**: Ensure `Cors__AllowedOrigins__10` environment variable in API Gateway includes your CloudFront URL

#### 401 Unauthorized

```
[API] Request failed: { status: 401 }
```

**Possible Causes**:

- JWT token not being sent
- JWT token expired
- Cognito configuration issue

**Check**: Look for `[API] Added auth token to request` vs `[API] No auth token available`

#### 500 Internal Server Error

```
[API] Request failed: { status: 500, data: {...} }
```

**Check CloudWatch Logs** in AWS:

- API Gateway logs: `/aws/apprunner/sri-template-api-gateway`
- Look for exceptions, stack traces

#### Network Error (No Response)

```
[SettingsStore] Failed to load settings: No response from server
```

**Possible Causes**:

- API Gateway not running
- Wrong API URL
- Network connectivity issue

### Step 3: Verify API Gateway Configuration

Check these environment variables in AWS App Runner (API Gateway):

```bash
# Required environment variables
ASPNETCORE_ENVIRONMENT=Production  # or Development
Services__IdentityService=<identity-service-url>
Services__DataService=<data-service-url>
Cors__AllowedOrigins__10=<cloudfront-url>
```

### Step 4: Test Settings Endpoint Directly

Using curl or Postman, test the endpoint:

```bash
# Without authentication (should work)
curl https://<api-gateway-url>/api/v1/users/settings

# Expected Response:
{
  "preferredUnits": "SI"
}
```

If this fails, the issue is in the backend, not the frontend.

### Step 5: Check CloudWatch Logs

Look for these log entries in AWS CloudWatch:

**API Gateway Startup:**

```
Starting ApiGateway...
Using CognitoJwtService for production
ApiGateway started successfully
```

**Request Logs:**

```
HTTP GET /api/v1/users/settings responded 200 in X.XXXX ms
```

**Error Logs:**

```
Error validating JWT token for /api/v1/users/settings
```

## Expected Behavior After Fix

### Success Case

1. User clicks Hydrostatics card
2. Console shows:
   ```
   [SettingsStore] Loading settings from API...
   [API] Making GET request...
   [API] Response received: { status: 200, data: { preferredUnits: "SI" } }
   [SettingsStore] Settings loaded successfully: { preferredUnits: "SI" }
   ```
3. App loads normally

### Failure Case (Graceful)

1. User clicks Hydrostatics card
2. Console shows:
   ```
   [SettingsStore] Loading settings from API...
   [API] Making GET request...
   [API] Request failed: { status: 500, ... }
   [SettingsStore] Failed to load settings - Full error: { ... }
   [SettingsStore] Using default settings due to error: ...
   ```
3. **App continues to work** with default SI units
4. User sees no error dialog (silent failure with defaults)

## Testing Checklist

After deploying the updated frontend to AWS:

- [ ] Open browser console (F12)
- [ ] Navigate to dashboard
- [ ] Click on Hydrostatics card
- [ ] Check console for detailed logs
- [ ] Verify app loads even if settings fail
- [ ] Check CloudWatch logs for API Gateway errors
- [ ] Test settings endpoint directly via curl
- [ ] Verify CORS headers in response

## Common Issues and Solutions

### Issue: "rt" Error Still Appears

**Solution**: The new error handling should replace this with a proper error message. If it still appears, check:

- Frontend deployment completed successfully
- Browser cache cleared (hard refresh: Ctrl+Shift+R)
- Service worker not caching old version

### Issue: Infinite Loading

**Solution**: The timeout is set to 10 seconds, so this shouldn't happen. If it does:

- Check if API Gateway is running in AWS
- Verify network connectivity
- Check CloudWatch logs for backend errors

### Issue: CORS Error

**Solution**: Update API Gateway environment variable:

```
Cors__AllowedOrigins__10=https://your-cloudfront-domain.cloudfront.net
```

Then restart API Gateway service in App Runner.

### Issue: Settings Not Persisting

**Note**: Settings persistence is not yet implemented (see TODO in backend). Currently returns default "SI" every time. This is expected behavior.

## Deployment Instructions

### 1. Deploy Frontend

```bash
cd frontend
npm run build

# Copy dist/ folder to S3 bucket
# Or use GitHub Actions workflow
```

### 2. Invalidate CloudFront Cache

```bash
aws cloudfront create-invalidation \
  --distribution-id YOUR_DIST_ID \
  --paths "/*"
```

### 3. Verify Deployment

```bash
# Check that new bundle is loaded
curl https://your-cloudfront-url/assets/index-*.js | grep "SettingsStore"
# Should see the new logging statements
```

## Next Steps

Once you've deployed and tested:

1. **Share console logs** - Copy the full console output when the error occurs
2. **Share CloudWatch logs** - Get the API Gateway logs for the same time period
3. **Share network tab** - Export HAR file from browser DevTools showing the failed request

With these logs, we can pinpoint the exact cause of the error in AWS.

## Contact Points

If the issue persists after these changes:

1. Verify the exact error message in console (should be more detailed now)
2. Check if the endpoint works directly via curl
3. Review CloudWatch logs for backend exceptions
4. Verify environment variables are set correctly in App Runner

The enhanced logging will provide much better diagnostic information to identify the root cause.
