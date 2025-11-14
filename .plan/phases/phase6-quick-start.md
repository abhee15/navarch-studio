# Phase 6: Authentication - Quick Start

This is a condensed version of Phase 6 for quick reference. **For detailed explanations, see `phase6-auth.md`**.

## TL;DR - What Phase 6 Does

Implements **real AWS Cognito authentication** to replace mock authentication:
- ✅ Frontend: Direct Cognito SDK integration (no Amplify bloat)
- ✅ Backend: JWT validation using Cognito public keys
- ✅ Protected routes and API endpoints

## Prerequisites

- Phase 5 completed (AWS infrastructure deployed)
- Cognito User Pool created (from Phase 4)

## Implementation Summary

### Frontend (`amazon-cognito-identity-js`)
```typescript
// Package: amazon-cognito-identity-js (~50KB)
// Files created/updated:
- frontend/src/config/cognito.ts          (Cognito UserPool config)
- frontend/src/stores/AuthStore.ts        (Signup, login, logout)
- frontend/src/services/api.ts            (JWT tokens in requests)
- frontend/src/pages/SignupPage.tsx       (User registration)
- frontend/src/App.tsx                    (Protected routes)
- frontend/package.json                   (Added dependency)
```

### Backend (.NET JWT Validation)
```csharp
// Files created:
- backend/Shared/Services/IJwtService.cs
- backend/Shared/Services/CognitoJwtService.cs
- backend/Shared/Middleware/JwtAuthenticationMiddleware.cs

// Files updated:
- backend/IdentityService/Program.cs      (JWT middleware)
- backend/DataService/Program.cs          (JWT middleware)
- backend/ApiGateway/Program.cs           (JWT middleware)
- backend/IdentityService/Controllers/UsersController.cs  (GET /me endpoint)
- backend/DataService/Controllers/ProductsController.cs   (Auth required for POST)
```

## Configuration

### Environment Variables

**Frontend** (`.env`):
```bash
VITE_COGNITO_USER_POOL_ID=us-east-1_XXXXXXXXX
VITE_COGNITO_CLIENT_ID=xxxxxxxxxxxxxxxxxxxxxxxxxx
VITE_AWS_REGION=us-east-1
```

**Backend** (all services - appsettings.json or env vars):
```json
{
  "Cognito": {
    "UserPoolId": "us-east-1_XXXXXXXXX",
    "ClientId": "xxxxxxxxxxxxxxxxxxxxxxxxxx",
    "Region": "us-east-1"
  }
}
```

### Docker Compose

Created `docker-compose.override.yml` with Cognito environment variables for local development.

## Key Features

### 1. **Frontend Authentication Flow**
```
User → Signup → Cognito User Pool (verification email)
User → Login → Get JWT Token → Store in Cognito Session
API Call → Get JWT from session → Add to Authorization header
```

### 2. **Backend JWT Validation**
```
Request → Extract Bearer token → Validate with Cognito public keys
        → Set HttpContext.User with claims → Controller access via User.Claims
```

### 3. **New API Endpoints**
- `GET /api/v1/users/me` - Get current authenticated user
- All `POST` endpoints now require authentication

## Testing Locally

```powershell
# 1. Install frontend dependencies
cd frontend
npm install

# 2. Set environment variables (create .env file)
VITE_COGNITO_USER_POOL_ID=your-pool-id
VITE_COGNITO_CLIENT_ID=your-client-id

# 3. Run docker-compose with override
docker-compose up

# 4. Test signup flow
# - Go to http://localhost:3000/signup
# - Create account
# - Check email for verification
# - Login at http://localhost:3000/login
```

## Bundle Size Impact

**Before Phase 6**: ~2.5MB  
**After Phase 6**: ~2.55MB (+50KB for amazon-cognito-identity-js)

## AWS Costs

**No additional costs!** Cognito pricing:
- First 50,000 MAUs/month: **FREE**
- After that: $0.0055 per MAU

For most development/small projects: **$0/month**

## What Changed

| Component | Before | After |
|-----------|--------|-------|
| **Frontend Auth** | Mock (localStorage) | Real Cognito SDK |
| **Backend Auth** | None | JWT validation |
| **User Sessions** | Client-side only | Cognito-managed |
| **Token Management** | Manual | Automatic refresh |
| **Security** | ⚠️ Insecure | ✅ Production-ready |

## Common Issues

### ❌ "NotAuthorizedException: Incorrect username or password"
**Cause:** Email not verified or wrong credentials  
**Solution:** Check email for verification link

### ❌ "User pool does not exist"
**Cause:** Wrong COGNITO_USER_POOL_ID  
**Solution:** Get correct value from Phase 4 Terraform outputs:
```powershell
cd terraform/setup
terraform output cognito_user_pool_id
```

### ❌ "CORS error when calling Cognito"
**Cause:** Cognito CORS not configured  
**Solution:** Cognito API calls use HTTPS directly - no CORS issues

### ❌ JWT validation failing
**Cause:** Wrong region or client ID  
**Solution:** Verify Cognito configuration matches in frontend and backend

## Next Steps

1. ✅ Authentication implemented
2. ➡️ Proceed to [Phase 7: CI/CD Pipeline](phase7-cicd.md)

---

## 📚 Additional Documentation

| Document | Purpose |
|----------|---------|
| **[phase6-auth.md](phase6-auth.md)** | Full detailed guide with all code examples |
| **[IAM_SETUP.md](IAM_SETUP.md)** | IAM permissions (no changes needed) |
| `terraform/IAM_POLICY_README.md` | IAM policy reference |

---

**📌 This is a QUICK REFERENCE guide** - Read `phase6-auth.md` first if it's your first time!






