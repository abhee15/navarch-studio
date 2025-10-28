# Environment Configuration Guide

## Overview

This application uses **different authentication and configuration strategies** for different environments:

- **Local Development**: JWT-based auth with local backend services
- **AWS Deployments (Dev/Staging/Prod)**: AWS Cognito authentication

## Environment Differentiation

### How GitHub Actions Differentiates Environments

Each GitHub Actions workflow is configured with environment-specific settings:

| Workflow         | Environment  | Trigger                                | Auth Mode | State File                         |
| ---------------- | ------------ | -------------------------------------- | --------- | ---------------------------------- |
| `ci-dev.yml`     | `dev`        | Push to `main`                         | Cognito   | `deploy/dev/terraform.tfstate`     |
| `ci-staging.yml` | `staging`    | Manual dispatch                        | Cognito   | `deploy/staging/terraform.tfstate` |
| `ci-prod.yml`    | `production` | Manual dispatch (requires version tag) | Cognito   | `deploy/prod/terraform.tfstate`    |

### Environment Variables by Environment

#### Local Development (`docker compose up`)

```yaml
# docker-compose.yml
environment:
  - VITE_API_URL=http://localhost:5002
  - VITE_AUTH_MODE=local # ← Uses local JWT auth
```

**Frontend uses**:

- Local IdentityService (port 5001)
- Local API Gateway (port 5002)
- Local JWT tokens (no AWS required)

#### Dev Environment (Automatic on push to main)

```yaml
# .github/workflows/ci-dev.yml
env:
  AWS_REGION: us-east-1
  ENVIRONMENT: dev

# Frontend build step:
VITE_API_URL: https://xxx-dev.awsapprunner.com
VITE_AUTH_MODE: cognito # ← Uses AWS Cognito
VITE_COGNITO_USER_POOL_ID: us-east-1_xxxxxxx
VITE_COGNITO_CLIENT_ID: xxxxxxxxxxxxx
VITE_AWS_REGION: us-east-1
```

**Frontend uses**:

- AWS App Runner services (dev-xxx)
- AWS Cognito User Pool
- AWS RDS (dev database)
- CloudFront + S3 for static assets

#### Staging Environment (Manual deployment)

```yaml
# .github/workflows/ci-staging.yml
env:
  AWS_REGION: us-east-1
  ENVIRONMENT: staging
# Same structure as dev but:
# - Separate terraform state: deploy/staging/terraform.tfstate
# - Separate AWS resources (staging-xxx)
# - Can use same or different Cognito pool
```

#### Production Environment (Manual with version tag)

```yaml
# .github/workflows/ci-prod.yml
env:
  AWS_REGION: us-east-1
  ENVIRONMENT: prod
# Same structure as staging but:
# - Requires explicit version tag (e.g., v1.0.0)
# - Separate terraform state: deploy/prod/terraform.tfstate
# - Production AWS resources (prod-xxx)
# - Additional approval gates (GitHub environment protection)
```

## Configuration Files

### Frontend Environment Files

```
frontend/
├── .env.example              # Template (not used at runtime)
├── .env.development          # Local dev defaults (VITE_AUTH_MODE=local)
└── .env.production           # Not used - GitHub Actions sets vars at build time
```

**Important**: Environment variables are **baked into the frontend bundle at build time** (Vite limitation). This is why GitHub Actions sets them during the build step.

### Terraform Environment Files

```
terraform/deploy/
├── environments/
│   ├── dev.tfvars           # Dev-specific settings (instance sizes, etc.)
│   ├── staging.tfvars       # Staging-specific settings
│   └── prod.tfvars          # Production-specific settings
└── backend-config.tfvars    # Created dynamically (points to correct state file)
```

## Authentication Flow by Environment

### Local Development

```
┌─────────────┐
│   Browser   │
└──────┬──────┘
       │ 1. Login (email/password)
       ↓
┌─────────────────────┐
│ Local IdentityService│ ← Uses local PostgreSQL
└──────┬───────────────┘
       │ 2. Returns JWT token
       ↓
┌─────────────┐
│ LocalStorage│ ← Stores JWT
└──────┬──────┘
       │ 3. Sends JWT in Authorization header
       ↓
┌──────────────┐
│ API Gateway  │ ← Validates JWT
└──────────────┘
```

**Key Points**:

- No AWS services required
- Password stored in local PostgreSQL with BCrypt hashing
- JWT signed with local secret key
- No email verification required

### AWS Environments (Dev/Staging/Prod)

```
┌─────────────┐
│   Browser   │
└──────┬──────┘
       │ 1. Login (email/password)
       ↓
┌─────────────────┐
│  AWS Cognito    │ ← Managed by AWS
└──────┬──────────┘
       │ 2. Returns Cognito session + tokens
       ↓
┌─────────────┐
│ Cognito SDK │ ← Manages session automatically
└──────┬──────┘
       │ 3. Sends Cognito JWT in Authorization header
       ↓
┌──────────────┐
│ API Gateway  │ ← Validates Cognito JWT
└──────────────┘
```

**Key Points**:

- AWS Cognito handles user management
- Email verification required (configurable)
- MFA support available
- Automatic token refresh
- Password policies managed by Cognito

## How to Switch Between Environments

### Running Locally

```bash
# Start with local auth
docker compose up

# Access: http://localhost:3000
# Login with: admin@example.com / password
```

### Deploying to Dev

```bash
# Automatic on push to main
git push origin main

# GitHub Actions will:
# 1. Build with VITE_AUTH_MODE=cognito
# 2. Deploy to AWS dev environment
# 3. Use dev-specific resources
```

### Deploying to Staging

```bash
# Manual trigger from GitHub UI
# Actions → ci-staging.yml → Run workflow

# Or via API:
gh workflow run ci-staging.yml
```

### Deploying to Production

```bash
# 1. Create a version tag
git tag v1.0.0
git push origin v1.0.0

# 2. Manual trigger with version
# Actions → ci-prod.yml → Run workflow → Enter version: v1.0.0
```

## Security Considerations

### Local Development

✅ **Safe for development**:

- No production data
- No production credentials
- Isolated environment
- Fast iteration

⚠️ **Not for production**:

- Simpler password requirements
- No email verification
- No MFA
- Single secret key (not rotated)

### AWS Environments

✅ **Production-ready**:

- AWS Cognito (enterprise-grade)
- Email verification
- MFA support
- Password policies enforced
- Automatic token rotation
- Audit logging via CloudTrail

## Troubleshooting

### Problem: Login works locally but not in Dev

**Cause**: Frontend built with wrong auth mode.

**Solution**:

1. Check GitHub Actions logs for environment variables
2. Verify `VITE_AUTH_MODE=cognito` is set during build
3. Check CloudFront cache invalidation completed

### Problem: Frontend shows Cognito errors locally

**Cause**: Using production build locally or missing `.env.development`.

**Solution**:

```bash
# 1. Ensure .env.development exists
echo "VITE_AUTH_MODE=local" > frontend/.env.development

# 2. Rebuild containers
docker compose up --build

# 3. Clear browser localStorage
# DevTools → Application → Local Storage → Clear All
```

### Problem: Different behavior in Dev vs Staging

**Cause**: Environments should be identical except for resource names.

**Solution**:

1. Compare `terraform/deploy/environments/dev.tfvars` vs `staging.tfvars`
2. Check GitHub secrets are set correctly for both
3. Verify Cognito pool configuration matches

## Best Practices

### 1. Always Test Locally First

```bash
docker compose up
# Test feature
docker compose down
```

### 2. Deploy to Dev Automatically

- Push to `main` triggers dev deployment
- Catches integration issues early

### 3. Manual Staging Deployments

- Deploy to staging before production
- Run full QA cycle
- Test with production-like data

### 4. Controlled Production Releases

- Always use version tags
- Require approval (GitHub environment protection)
- Monitor after deployment

### 5. Keep Secrets Secure

```bash
# ❌ NEVER commit secrets
echo "VITE_COGNITO_CLIENT_ID=abc123" >> .env.production

# ✅ Use GitHub Secrets
gh secret set COGNITO_CLIENT_ID -b"abc123"
```

## Environment Variable Reference

| Variable                    | Local                   | Dev                                | Staging                                | Prod                      | Description             |
| --------------------------- | ----------------------- | ---------------------------------- | -------------------------------------- | ------------------------- | ----------------------- |
| `VITE_API_URL`              | `http://localhost:5002` | `https://xxx-dev.awsapprunner.com` | `https://xxx-staging.awsapprunner.com` | `https://api.example.com` | Backend API URL         |
| `VITE_AUTH_MODE`            | `local`                 | `cognito`                          | `cognito`                              | `cognito`                 | Authentication strategy |
| `VITE_COGNITO_USER_POOL_ID` | (not set)               | `us-east-1_dev123`                 | `us-east-1_stg456`                     | `us-east-1_prd789`        | Cognito User Pool ID    |
| `VITE_COGNITO_CLIENT_ID`    | (not set)               | `abc123dev`                        | `abc123stg`                            | `abc123prd`               | Cognito App Client ID   |
| `VITE_AWS_REGION`           | (not set)               | `us-east-1`                        | `us-east-1`                            | `us-east-1`               | AWS Region              |

## Related Documentation

- [Local Development Guide](./LOCAL_DEVELOPMENT.md) - Running locally
- [Deployment Workflow](./DEPLOYMENT_WORKFLOW.md) - AWS deployments
- [GitHub Secrets](./GITHUB_SECRETS.md) - Setting up secrets
- [Architecture](./ARCHITECTURE.md) - System design
