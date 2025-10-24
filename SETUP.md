# Sri-Subscription Setup Documentation

**Created**: October 23, 2025  
**Project**: sri-subscription  
**Template**: sri-template  
**Owner**: abhee15

## Overview

This document details the automated setup of `sri-subscription` as an independent project forked from `sri-template`, configured for cost-effective parallel development with shared foundation infrastructure.

## Architecture Strategy

### Shared Resources (Cost Optimization)

- **VPC & Subnets**: Reused from sri-template (free)
- **Security Groups**: Reused from sri-template (free)
- **Cognito User Pool**: Reused from sri-template (free <50k MAU)
  - Users can authenticate to both apps with same credentials
  - Sri-subscription has its own Cognito app client

### Isolated Resources (Per-Project)

- **ECR Repositories**: sri-subscription-\* (separate images)
- **RDS Database**: sri-subscription-dev-db (separate data)
- **App Runner Services**: sri-subscription-\* (separate compute)
- **S3 + CloudFront**: sri-subscription-\* (separate frontend)

### Cost Comparison

```
Single Project (sri-template): ~$32/month
- VPC: $0
- Cognito: $0
- RDS t3.micro: ~$15/mo
- App Runner (3 services): ~$15/mo
- S3 + CloudFront: ~$2/mo

Two Projects (shared foundation): ~$64/month
- Shared VPC: $0
- Shared Cognito: $0
- RDS t3.micro x2: ~$30/mo
- App Runner x6: ~$30/mo
- S3 + CloudFront x2: ~$4/mo

Monthly cost per project: $32 (same as standalone)
Benefit: Shared authentication, centralized user management
```

## Setup Commands Executed

### 1. Repository Creation

```powershell
# Create directory
New-Item -ItemType Directory -Path "C:\Abhi\Projects\Sri\sri-subscription" -Force

# Copy files from template (excluding build artifacts)
robocopy "C:\Abhi\Projects\Sri\sri-template" "C:\Abhi\Projects\Sri\sri-subscription" /E /XD .git node_modules bin obj .terraform /XF *.tfstate *.tfstate.backup

cd C:\Abhi\Projects\Sri\sri-subscription
```

### 2. Placeholder Replacement

```powershell
# Rename solution file
Rename-Item -Path "backend\{{PROJECT_NAME}}.sln" -NewName "sri-subscription.sln"

# Replace all placeholders
Get-ChildItem -Recurse -File | Where-Object { $_.Extension -match '\.(md|json|yml|yaml|tf|cs|ts|tsx|js|jsx|ps1|sh|sln|csproj|html|css|txt)$' } | ForEach-Object {
    (Get-Content $_.FullName -Raw) -replace '{{PROJECT_NAME}}', 'sri-subscription' |
    Set-Content $_.FullName
}

# Replace other placeholders
# AWS_REGION → us-east-1
# AWS_ACCOUNT_ID → 344870914438
# GITHUB_OWNER → abhee15
# DOMAIN_NAME → (empty)
```

### 3. Port Configuration (Conflict Isolation)

**Updated Files**:

- `docker-compose.yml`:

  - Postgres: 5432 → 5433
  - PgAdmin: 5050 → 5051
  - Identity: 5001 → 5011
  - API Gateway: 5002 → 5012
  - Data Service: 5003 → 5013
  - Frontend: 3000 → 3001

- `frontend/vite.config.ts`:

  - Port: 3000 → 3001
  - Proxy target: 5002 → 5012

- `backend/*/Properties/launchSettings.json`:

  - Identity Service: 5011
  - API Gateway: 5012
  - Data Service: 5013

- Database names in `docker-compose.yml`:
  - sri_template_dev → sri_subscription_dev

### 4. Terraform Configuration

**Created Files**:

`terraform/setup/shared-resources.tf`:

- Data sources to reference existing sri-template VPC, subnets, security groups
- Data source for existing Cognito User Pool
- Outputs shared resource IDs for deploy phase

`terraform/setup/cognito-client.tf`:

- Creates NEW Cognito app client in shared user pool
- Client name: sri-subscription-client
- Separate client ID from sri-template

**Existing Terraform files automatically use project_name variable** to create:

- sri-subscription-\* ECR repositories
- sri-subscription-\* terraform state bucket
- sri-subscription-\* DynamoDB lock table

### 5. Git Configuration

```bash
# Initialize repository
git init
git config user.email "abhee15@gmail.com"
git config user.name "abhee15"

# Add all files
git add .

# Initial commit
git commit -m "Initial setup from sri-template for sri-subscription project"

# Setup branches
git branch -M main
git checkout -b develop

# Add template remote for future syncs
git remote add template https://github.com/abhee15/sri-template.git
```

### 6. Config Protection

**Created Files**:

`.gitattributes`:

- Protects project-specific configs from template syncs
- Files marked with `merge=ours` keep sri-subscription version
- Files marked with `merge=union` trigger conflicts for review

`scripts/sync-from-template.ps1`:

- Automated script to sync updates from sri-template
- Backs up protected configs before merge
- Restores sri-subscription configs after merge

`docs/TEMPLATE_SYNC.md`:

- Complete guide for syncing template updates
- Lists all files that must never be overwritten
- Provides troubleshooting steps

## Next Steps

### 1. Create GitHub Repository

**Manual step required** (GitHub CLI not installed):

```bash
# Option 1: GitHub Web Interface
1. Go to https://github.com/new
2. Repository name: sri-subscription
3. Description: "Subscription management system forked from sri-template"
4. Create repository (public or private)

# Option 2: GitHub CLI (if installed)
gh repo create sri-subscription --public --source=. --remote=origin --push
```

### 2. Push to GitHub

```bash
cd c:\Abhi\Projects\Sri\sri-subscription

# Add remote
git remote add origin https://github.com/abhee15/sri-subscription.git

# Push branches
git push -u origin main
git push -u origin develop
```

### 3. Configure GitHub Secrets

Required secrets for CI/CD:

```
AWS_ACCESS_KEY_ID = <your-access-key>
AWS_SECRET_ACCESS_KEY = <your-secret-key>
AWS_REGION = us-east-1
AWS_ACCOUNT_ID = 344870914438

# Will be generated after terraform apply:
S3_BUCKET_NAME = sri-subscription-terraform-state
DYNAMODB_TABLE_NAME = sri-subscription-terraform-locks
VPC_ID = <from sri-template, shared>
PUBLIC_SUBNET_IDS = <from sri-template, shared>
APP_RUNNER_SECURITY_GROUP_ID = <from sri-template, shared>
RDS_SECURITY_GROUP_ID = <from sri-template, shared>
COGNITO_USER_POOL_ID = us-east-1_WTfHVTfHT (shared)
COGNITO_USER_POOL_CLIENT_ID = <new, from terraform output>
COGNITO_DOMAIN = sri-test-project-1-1zvox1e6 (shared)

# ECR URLs (new, from terraform):
ECR_IDENTITY_SERVICE_URL = 344870914438.dkr.ecr.us-east-1.amazonaws.com/sri-subscription-identity-service
ECR_API_GATEWAY_URL = 344870914438.dkr.ecr.us-east-1.amazonaws.com/sri-subscription-api-gateway
ECR_DATA_SERVICE_URL = 344870914438.dkr.ecr.us-east-1.amazonaws.com/sri-subscription-data-service
ECR_FRONTEND_URL = 344870914438.dkr.ecr.us-east-1.amazonaws.com/sri-subscription-frontend
```

### 4. Run Terraform Setup

```powershell
cd terraform/setup

# Verify configuration
cat terraform.tfvars
# Should show:
#   project_name   = "sri-subscription"
#   aws_region     = "us-east-1"
#   aws_account_id = "344870914438"
#   cost_center    = "engineering"
#   budget_email   = "abhee15@gmail.com"

# Initialize terraform
terraform init

# Review plan (should reference shared resources)
terraform plan

# Apply (creates new resources with sri-subscription prefix)
terraform apply

# Get outputs for GitHub secrets
terraform output -json
```

### 5. Run Terraform Deploy (Dev Environment)

```powershell
cd ../deploy

# Copy backend config
cp backend-config.tfvars.example backend-config.tfvars
# Edit to use sri-subscription state bucket from setup output

# Initialize with backend
terraform init -backend-config=backend-config.tfvars

# Plan for dev environment
terraform plan -var-file=environments/dev.tfvars

# Apply
terraform apply -var-file=environments/dev.tfvars

# Get deployment URLs
terraform output
```

### 6. Test Local Development

Both projects can run simultaneously:

```powershell
# Terminal 1: Sri-template
cd c:\Abhi\Projects\Sri\sri-template
docker-compose up
# Access: http://localhost:3000

# Terminal 2: Sri-subscription
cd c:\Abhi\Projects\Sri\sri-subscription
docker-compose up
# Access: http://localhost:3001
```

### 7. Sync Future Template Updates

When sri-template is updated:

```powershell
cd c:\Abhi\Projects\Sri\sri-subscription

# Automated sync (recommended)
.\scripts\sync-from-template.ps1

# Manual sync (if needed)
git fetch template
git checkout -b sync-template-updates
git merge template/main
# Resolve conflicts keeping sri-subscription configs
git checkout develop
git merge sync-template-updates
```

## Port Mapping Summary

### Sri-Template (Original)

- Frontend: `localhost:3000`
- Identity Service: `localhost:5001`
- API Gateway: `localhost:5002`
- Data Service: `localhost:5003`
- Postgres: `localhost:5432`
- PgAdmin: `localhost:5050`

### Sri-Subscription (New)

- Frontend: `localhost:3001`
- Identity Service: `localhost:5011`
- API Gateway: `localhost:5012`
- Data Service: `localhost:5013`
- Postgres: `localhost:5433`
- PgAdmin: `localhost:5051`

## Key Files Modified

- `backend/sri-subscription.sln` (renamed from {{PROJECT_NAME}}.sln)
- `docker-compose.yml` (updated ports, database name)
- `frontend/vite.config.ts` (port 3001, proxy to 5012)
- `backend/*/Properties/launchSettings.json` (updated ports)
- `terraform/setup/terraform.tfvars` (project_name = "sri-subscription")
- `.gitattributes` (NEW - config protection)
- `terraform/setup/shared-resources.tf` (NEW - reference shared resources)
- `terraform/setup/cognito-client.tf` (NEW - new app client)
- `scripts/sync-from-template.ps1` (NEW - sync automation)
- `docs/TEMPLATE_SYNC.md` (NEW - sync guide)

## Validation Checklist

- ✅ All placeholders replaced
- ✅ Solution file renamed
- ✅ Ports updated for parallel development
- ✅ Database names updated
- ✅ Terraform configured for shared resources
- ✅ Git initialized with main and develop branches
- ✅ Template remote configured
- ✅ Config protection implemented
- ⏳ GitHub repository creation (manual step)
- ⏳ GitHub secrets configuration (after terraform apply)
- ⏳ Terraform setup apply
- ⏳ Terraform deploy apply
- ⏳ Local testing

## Support

For issues or questions:

1. Check `docs/TEMPLATE_SYNC.md` for sync-related issues
2. Review terraform outputs for infrastructure details
3. Verify GitHub workflows status
4. Test locally with `docker-compose up`

## References

- Sri-template repository: https://github.com/abhee15/sri-template
- AWS Region: us-east-1
- Shared Cognito Pool: us-east-1_WTfHVTfHT
- Shared VPC: From sri-template terraform state
