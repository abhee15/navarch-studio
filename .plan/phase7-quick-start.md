# Phase 7: CI/CD Quick Start Guide

This is a condensed version of Phase 7 for quick reference. **For detailed explanations, see `phase7-cicd.md`**.

## TL;DR - Three-Step Setup

### Step 1: Configure GitHub Secrets

All GitHub secrets need to be added **once** for CI/CD to work. Use either GitHub UI or CLI:

#### Option A: Using GitHub CLI (Fastest)

```bash
# Navigate to terraform/setup to fetch outputs
cd terraform/setup

# Set AWS credentials (from IAM user)
gh secret set AWS_ACCESS_KEY_ID --body "YOUR_ACCESS_KEY"
gh secret set AWS_SECRET_ACCESS_KEY --body "YOUR_SECRET_KEY"

# Set project configuration
gh secret set PROJECT_NAME --body "sri-template"
gh secret set COST_CENTER --body "engineering"
gh secret set DOMAIN_NAME --body ""

# Auto-fetch from Terraform outputs
gh secret set S3_BUCKET_NAME --body "$(terraform output -raw s3_bucket_name)"
gh secret set DYNAMODB_TABLE_NAME --body "$(terraform output -raw dynamodb_table_name)"
gh secret set VPC_ID --body "$(terraform output -raw vpc_id)"
gh secret set PUBLIC_SUBNET_IDS --body "$(terraform output -json public_subnet_ids)"
gh secret set APP_RUNNER_SECURITY_GROUP_ID --body "$(terraform output -raw app_runner_security_group_id)"
gh secret set RDS_SECURITY_GROUP_ID --body "$(terraform output -raw rds_security_group_id)"

# ECR repository URLs
gh secret set ECR_IDENTITY_SERVICE_URL --body "$(terraform output -json ecr_repository_urls | jq -r '.identity_service')"
gh secret set ECR_API_GATEWAY_URL --body "$(terraform output -json ecr_repository_urls | jq -r '.api_gateway')"
gh secret set ECR_DATA_SERVICE_URL --body "$(terraform output -json ecr_repository_urls | jq -r '.data_service')"
gh secret set ECR_FRONTEND_URL --body "$(terraform output -json ecr_repository_urls | jq -r '.frontend')"

# Cognito
gh secret set COGNITO_USER_POOL_ID --body "$(terraform output -raw cognito_user_pool_id)"
gh secret set COGNITO_USER_POOL_CLIENT_ID --body "$(terraform output -raw cognito_user_pool_client_id)"

# Database credentials (set your own secure password)
gh secret set RDS_DATABASE --body "sri_template_db"
gh secret set RDS_USERNAME --body "postgres"
gh secret set RDS_PASSWORD --body "YOUR_SECURE_PASSWORD_HERE"
```

#### Option B: Using PowerShell (Windows)

```powershell
cd terraform/setup

# AWS credentials
gh secret set AWS_ACCESS_KEY_ID --body "YOUR_ACCESS_KEY"
gh secret set AWS_SECRET_ACCESS_KEY --body "YOUR_SECRET_KEY"

# Project config
gh secret set PROJECT_NAME --body "sri-template"
gh secret set COST_CENTER --body "engineering"
gh secret set DOMAIN_NAME --body ""

# Terraform outputs
gh secret set S3_BUCKET_NAME --body (terraform output -raw s3_bucket_name)
gh secret set DYNAMODB_TABLE_NAME --body (terraform output -raw dynamodb_table_name)
gh secret set VPC_ID --body (terraform output -raw vpc_id)
gh secret set PUBLIC_SUBNET_IDS --body (terraform output -json public_subnet_ids)
gh secret set APP_RUNNER_SECURITY_GROUP_ID --body (terraform output -raw app_runner_security_group_id)
gh secret set RDS_SECURITY_GROUP_ID --body (terraform output -raw rds_security_group_id)

# ECR URLs
$ecrUrls = terraform output -json ecr_repository_urls | ConvertFrom-Json
gh secret set ECR_IDENTITY_SERVICE_URL --body $ecrUrls.identity_service
gh secret set ECR_API_GATEWAY_URL --body $ecrUrls.api_gateway
gh secret set ECR_DATA_SERVICE_URL --body $ecrUrls.data_service
gh secret set ECR_FRONTEND_URL --body $ecrUrls.frontend

# Cognito
gh secret set COGNITO_USER_POOL_ID --body (terraform output -raw cognito_user_pool_id)
gh secret set COGNITO_USER_POOL_CLIENT_ID --body (terraform output -raw cognito_user_pool_client_id)

# Database
gh secret set RDS_DATABASE --body "sri_template_db"
gh secret set RDS_USERNAME --body "postgres"
gh secret set RDS_PASSWORD --body "YOUR_SECURE_PASSWORD_HERE"
```

**📖 Full list:** See `docs/GITHUB_SECRETS.md`

### Step 2: Configure Branch Protection

```bash
# Main branch (production-ready)
# Settings → Branches → Add rule → Branch name: "main"
# ✅ Require a pull request before merging (1 approval)
# ✅ Require status checks: frontend-checks, backend-checks, terraform-checks, security-checks, docker-build-checks
# ✅ Require linear history
# ✅ Include administrators

# Develop branch (integration)
# Same as main, but only require: frontend-checks, backend-checks, terraform-checks
```

**📖 Full guide:** See `docs/BRANCH_PROTECTION.md`

### Step 3: Test CI/CD

```bash
# Create a test branch
git checkout -b feature/test-cicd

# Make a small change (e.g., update README.md)
echo "Testing CI/CD" >> README.md

# Commit and push
git add README.md
git commit -m "test: verify CI/CD pipeline"
git push -u origin feature/test-cicd

# Create a Pull Request to 'develop'
# ✅ PR checks should run automatically
# ✅ Wait for all checks to pass
# ✅ Request review and merge

# After merging to 'develop', dev deployment should trigger automatically
# Check: Actions → Deploy to Dev
```

---

## 🔄 What Happens Automatically

### On Pull Request

**Workflow:** `.github/workflows/pr-checks.yml`

- ✅ Frontend linting, type checking, tests, build
- ✅ Backend tests for all 3 services
- ✅ Terraform validation
- ✅ Security scanning with Trivy
- ✅ Docker image build checks

### On Push to `develop`

**Workflow:** `.github/workflows/ci-dev.yml`

1. Build and push Docker images to ECR (tagged with commit SHA + `latest`)
2. Deploy infrastructure using Terraform (RDS, App Runner, S3, CloudFront)
3. Run database migrations
4. Build and deploy frontend to S3
5. Invalidate CloudFront cache
6. Run smoke tests (health checks)
7. Notify deployment status

### On Push to `main`

**Workflow:** `.github/workflows/ci-staging.yml`

Same as dev, but:

- Images tagged with commit SHA + `staging`
- Deploys to staging environment
- Uses `terraform/deploy/environments/staging.tfvars`

### On Manual Trigger (Production)

**Workflow:** `.github/workflows/ci-prod.yml`

- **Manual only:** Go to Actions → Deploy to Production → Run workflow
- **Requires:** Version tag input (e.g., `v1.0.0`)
- **Requires:** Manual approval (configure in Settings → Environments → production)
- Uses `terraform/deploy/environments/prod.tfvars`
- Images tagged with version + `prod`

---

## 🧹 Destroying Environments (Cost Management)

To destroy an environment when not in use:

```bash
# Go to: Actions → Destroy Environment → Run workflow
# 1. Select environment: dev / staging / prod
# 2. Type "DESTROY" to confirm
# 3. Click "Run workflow"
```

**What gets destroyed:**

- ✅ RDS PostgreSQL instance
- ✅ App Runner services
- ✅ S3 frontend bucket (emptied and deleted)
- ✅ CloudFront distribution

**What remains (shared):**

- ⚠️ VPC, subnets, security groups (from Phase 4)
- ⚠️ ECR repositories
- ⚠️ Cognito User Pool
- ⚠️ Terraform state bucket/table

---

## 📋 Workflows Summary

| Workflow                | Trigger                          | Purpose                       | Auto-Deploy                          |
| ----------------------- | -------------------------------- | ----------------------------- | ------------------------------------ |
| **PR Checks**           | Pull request to `main`/`develop` | Quality gates                 | No                                   |
| **Dev Deploy**          | Push to `develop`                | Deploy to dev environment     | Yes                                  |
| **Staging Deploy**      | Push to `main`                   | Deploy to staging             | Yes                                  |
| **Prod Deploy**         | Manual + version tag             | Deploy to production          | No (manual approval)                 |
| **Destroy Environment** | Manual                           | Destroy environment resources | No (requires "DESTROY" confirmation) |

---

## 🚨 Common Issues

### ❌ "Secret not found" error

**Problem:** GitHub secret not configured.

**Solution:** Verify all secrets in `docs/GITHUB_SECRETS.md` are set. Check exact spelling (case-sensitive).

### ❌ "Required status check not found"

**Problem:** Branch protection configured before workflows exist.

**Solution:**

1. Merge workflows to `main` first
2. Trigger a test PR to create the status check
3. Then add to required status checks

### ❌ "Terraform state locked"

**Problem:** Previous Terraform run didn't complete or crashed.

**Solution:**

```bash
# From terraform/deploy
terraform force-unlock LOCK_ID
```

### ❌ "Access Denied" during deployment

**Problem:** IAM permissions insufficient.

**Solution:** Verify IAM user has all policies from `.plan/IAM_SETUP.md`.

---

## ✅ Verification Checklist

After Phase 7 setup:

- [ ] All GitHub secrets configured
- [ ] Branch protection rules active for `main` and `develop`
- [ ] CODEOWNERS file in place
- [ ] Test PR created and checks pass
- [ ] Dev deployment triggered and successful
- [ ] Smoke tests pass (all services healthy)
- [ ] Frontend accessible via CloudFront URL
- [ ] API endpoints responding correctly
- [ ] Database migrations applied

---

## 🧪 Testing Checkpoint

> **🎯 THIS IS A GOOD SPOT TO TEST YOUR SETUP!**

After completing Phase 7, you have a **fully automated CI/CD pipeline**. Here's what to test:

1. **Create a test PR** to `develop` → Verify all checks pass
2. **Merge to `develop`** → Verify dev deployment succeeds
3. **Check deployed app:**
   - Get CloudFront URL from workflow output
   - Open in browser
   - Test signup/login
   - Verify API calls work
4. **Destroy dev environment** (if not using) → Saves costs

**Next:** Proceed to Phase 8 for final polish and documentation.

---

## 📚 Additional Documentation

| Document                                                      | Purpose                                    |
| ------------------------------------------------------------- | ------------------------------------------ |
| **[phase7-cicd.md](phase7-cicd.md)**                          | Full detailed guide with all workflow code |
| **[docs/GITHUB_SECRETS.md](../docs/GITHUB_SECRETS.md)**       | Complete list of required secrets          |
| **[docs/BRANCH_PROTECTION.md](../docs/BRANCH_PROTECTION.md)** | Branch protection setup guide              |
| `.github/CODEOWNERS`                                          | Code ownership assignments                 |

---

**📌 This is a QUICK REFERENCE guide** - Read `phase7-cicd.md` first if it's your first time!





