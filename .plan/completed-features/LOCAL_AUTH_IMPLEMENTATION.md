# Local Authentication Implementation - Summary

## Problem Statement

When running `docker compose up` locally, the application was attempting to authenticate with **AWS Cognito** (production authentication) instead of using the local IdentityService backend. This caused errors:

```
cognito-idp.us-east-1.amazonaws.com: Failed to load resource: 400
Login failed: NotAuthorizedException: Incorrect username or password
Failed to load resource: 404 (Not Found) from vqfixuu7ry.us-east-1.awsapprunner.com
```

## Root Cause

The frontend was hardcoded to use AWS Cognito authentication regardless of environment. There was no mechanism to switch between:

- **Local JWT authentication** (for development with docker-compose)
- **AWS Cognito authentication** (for production deployments)

## Solution Overview

Implemented a **dual-mode authentication system** that automatically selects the appropriate authentication method based on the environment:

| Environment                 | Auth Mode | Backend         | User Store        |
| --------------------------- | --------- | --------------- | ----------------- |
| Local (`docker compose up`) | JWT       | IdentityService | PostgreSQL        |
| AWS (Dev/Staging/Prod)      | Cognito   | AWS Cognito     | Cognito User Pool |

## Changes Made

### 1. Created Local JWT Authentication Service

**File**: `frontend/src/services/localAuthService.ts` (NEW)

```typescript
export class LocalAuthService {
  static async login(email: string, password: string): Promise<LocalAuthUser>;
  static async signup(
    email: string,
    password: string,
    name: string
  ): Promise<void>;
  static async getCurrentUser(): Promise<LocalAuthUser>;
  static logout(): void;
  static getToken(): string | null;
  static isAuthenticated(): boolean;
}
```

This service:

- Calls local IdentityService API (`/api/v1/auth/login`, `/api/v1/users`)
- Stores JWT token in localStorage
- Manages user session for local development

### 2. Updated AuthStore with Dual-Mode Support

**File**: `frontend/src/stores/AuthStore.ts` (MODIFIED)

Added:

```typescript
type AuthMode = 'cognito' | 'local';
private authMode: AuthMode;

constructor() {
  this.authMode = (import.meta.env.VITE_AUTH_MODE || 'local') as AuthMode;
}
```

Updated methods to branch based on `authMode`:

- `initializeAuth()` - Check local storage OR Cognito session
- `login()` - Call LocalAuthService OR Cognito
- `signup()` - Call LocalAuthService OR Cognito
- `logout()` - Clear localStorage OR Cognito session
- `getIdToken()` - Return JWT from localStorage OR Cognito

### 3. Updated API Client

**File**: `frontend/src/services/api.ts` (MODIFIED)

```typescript
const getAuthMode = (): AuthMode => {
  return (import.meta.env.VITE_AUTH_MODE || "local") as AuthMode;
};

const getAuthToken = async (): Promise<string | null> => {
  const authMode = getAuthMode();
  if (authMode === "local") {
    return LocalAuthService.getToken();
  }
  return getCognitoToken(); // Existing Cognito logic
};
```

This ensures API requests use the correct token type based on environment.

### 4. Updated Docker Compose Configuration

**File**: `docker-compose.yml` (MODIFIED)

```yaml
frontend:
  environment:
    - VITE_API_URL=http://localhost:5002
    - VITE_AUTH_MODE=local # ← NEW: Forces local auth
```

### 5. Updated GitHub Actions Workflows

**Files**: `.github/workflows/ci-dev.yml`, `ci-staging.yml`, `ci-prod.yml` (MODIFIED)

Added environment variables to frontend build steps:

```yaml
env:
  VITE_API_URL: https://xxx.awsapprunner.com
  VITE_AUTH_MODE: cognito # ← NEW: Forces Cognito auth
  VITE_COGNITO_USER_POOL_ID: ${{ secrets.COGNITO_USER_POOL_ID }}
  VITE_COGNITO_CLIENT_ID: ${{ secrets.COGNITO_USER_POOL_CLIENT_ID }}
  VITE_AWS_REGION: ${{ env.AWS_REGION }}
```

### 6. Created Environment Configuration Files

**File**: `frontend/.env.development` (NEW)

```bash
VITE_API_URL=http://localhost:5002
VITE_AUTH_MODE=local
```

**File**: `frontend/.env.example` (NEW)

```bash
# API Configuration
VITE_API_URL=http://localhost:5002

# Authentication Mode: 'cognito' or 'local'
VITE_AUTH_MODE=local

# Cognito Configuration (only needed when VITE_AUTH_MODE=cognito)
VITE_COGNITO_USER_POOL_ID=
VITE_COGNITO_CLIENT_ID=
VITE_AWS_REGION=us-east-1
```

### 7. Created Documentation

**Files** (NEW):

- `docs/LOCAL_DEVELOPMENT.md` - Guide for running locally
- `docs/ENVIRONMENT_CONFIGURATION.md` - Complete environment reference

## How It Works

### Local Development Flow

```
1. User starts: docker compose up
   ↓
2. Frontend container starts with VITE_AUTH_MODE=local
   ↓
3. User navigates to http://localhost:3000
   ↓
4. AuthStore initializes with authMode='local'
   ↓
5. User clicks login, enters: admin@example.com / password
   ↓
6. AuthStore.login() → LocalAuthService.login()
   ↓
7. POST http://localhost:5002/api/v1/auth/login
   ↓
8. IdentityService validates against PostgreSQL
   ↓
9. Returns JWT token
   ↓
10. LocalAuthService stores token in localStorage
    ↓
11. API requests include: Authorization: Bearer <jwt-token>
    ↓
12. ✅ User is authenticated!
```

### Production Flow

```
1. GitHub Actions builds frontend with VITE_AUTH_MODE=cognito
   ↓
2. Frontend deployed to S3 + CloudFront
   ↓
3. User navigates to https://xxx.cloudfront.net
   ↓
4. AuthStore initializes with authMode='cognito'
   ↓
5. User clicks login, enters credentials
   ↓
6. AuthStore.login() → Cognito authentication
   ↓
7. AWS Cognito validates user
   ↓
8. Returns Cognito session tokens
   ↓
9. CognitoUserPool manages session
   ↓
10. API requests include: Authorization: Bearer <cognito-jwt>
    ↓
11. ✅ User is authenticated!
```

## Testing the Solution

### 1. Clean Start

```bash
# Stop and remove everything
docker compose down -v

# Rebuild containers
docker compose up --build
```

### 2. Test Local Authentication

```bash
# 1. Open browser: http://localhost:3000

# 2. Login with test credentials:
#    Email: admin@example.com
#    Password: password

# 3. Check DevTools → Console:
#    ✅ Should NOT see Cognito errors
#    ✅ Should see successful API calls to localhost:5002

# 4. Check DevTools → Application → Local Storage:
#    ✅ Should see 'jwt_token' key
#    ✅ Should see 'user_data' key
```

### 3. Verify API Calls

```bash
# Open DevTools → Network tab
# Login and check requests:

# ✅ Should see:
POST http://localhost:5002/api/v1/auth/login (200 OK)
GET http://localhost:5002/api/v1/users/me (200 OK)

# ❌ Should NOT see:
POST https://cognito-idp.us-east-1.amazonaws.com/
GET https://xxx.awsapprunner.com/
```

### 4. Test Signup

```bash
# 1. Go to signup page
# 2. Enter:
#    Email: test@example.com
#    Password: password123
#    Name: Test User
# 3. Submit
# 4. Should succeed WITHOUT email verification
# 5. Login with new credentials
```

## Test Users (Local Development)

The seed data includes:

| Email             | Password | Name       |
| ----------------- | -------- | ---------- |
| admin@example.com | password | Admin User |
| user@example.com  | password | Test User  |

## Verification Checklist

- [x] Local auth service created
- [x] AuthStore supports dual modes
- [x] API client uses correct tokens
- [x] docker-compose.yml sets VITE_AUTH_MODE=local
- [x] GitHub Actions workflows set VITE_AUTH_MODE=cognito
- [x] .env.development file created
- [x] Documentation created
- [x] Test users available in seed data

## Security Notes

### Local Development Security

✅ **Safe for local development:**

- BCrypt password hashing
- JWT tokens with expiration
- No hardcoded secrets
- Isolated from production

⚠️ **Not suitable for production:**

- Simplified password requirements
- No email verification
- No MFA
- Single secret key

### Production Security

✅ **Production-ready with Cognito:**

- AWS Cognito User Pool
- Email verification
- MFA support available
- Password policies enforced
- Automatic token rotation
- AWS CloudTrail audit logging

## Rollback Plan

If issues occur, rollback by:

```bash
# 1. Revert frontend changes
git revert <commit-hash>

# 2. Rebuild containers
docker compose up --build

# 3. Or use Cognito locally (temporary):
# Create frontend/.env.local:
VITE_AUTH_MODE=cognito
VITE_COGNITO_USER_POOL_ID=your-pool-id
VITE_COGNITO_CLIENT_ID=your-client-id
```

## Next Steps (Optional Enhancements)

1. **Rate Limiting**: Add rate limiting to login endpoint
2. **Audit Logging**: Log authentication events
3. **Token Refresh**: Automatic JWT refresh before expiration
4. **Session Timeout**: Warn user before session expires
5. **Remember Me**: Optional persistent sessions

## Related Files

### Modified Files

- `frontend/src/stores/AuthStore.ts`
- `frontend/src/services/api.ts`
- `docker-compose.yml`
- `.github/workflows/ci-dev.yml`
- `.github/workflows/ci-staging.yml`
- `.github/workflows/ci-prod.yml`

### New Files

- `frontend/src/services/localAuthService.ts`
- `frontend/.env.development`
- `frontend/.env.example`
- `docs/LOCAL_DEVELOPMENT.md`
- `docs/ENVIRONMENT_CONFIGURATION.md`
- `LOCAL_AUTH_IMPLEMENTATION.md` (this file)

## Success Criteria

✅ **Solution is successful when:**

1. `docker compose up` starts without Cognito errors
2. Login with local test users works
3. API calls go to localhost:5002
4. No 404 errors from AWS URLs
5. User can signup without email verification
6. Production deployments still use Cognito
7. Dev/Staging/Prod environments unaffected

## Support

For issues:

1. Check `docs/LOCAL_DEVELOPMENT.md` troubleshooting section
2. Verify environment variables: `docker compose config`
3. Check browser console for errors
4. Verify backend services are running: `docker compose ps`
5. Check backend logs: `docker compose logs api-gateway identity-service`
