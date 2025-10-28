# Deployment Prerequisites

## ⚠️ IMPORTANT: Phase 4 Must Be Completed First

Before the GitHub Actions CI/CD workflows can deploy your infrastructure, you **MUST** manually complete Phase 4 (AWS Infrastructure Setup). This creates the foundational resources that the workflows depend on.

## Why Phase 4 is Required

The deployment workflows need these resources to store and manage Terraform state:

1. **S3 Bucket** - Stores Terraform state files for each environment
2. **DynamoDB Table** - Provides state locking to prevent concurrent modifications
3. **VPC & Networking** - Core network infrastructure
4. **Security Groups** - Firewall rules for services
5. **ECR Repositories** - Docker image storage
6. **Cognito User Pool** - Authentication service
7. **CloudWatch Log Groups** - Centralized logging

## Setup Steps

### Step 1: Complete Phase 4 Locally

```powershell
# Navigate to setup directory
cd terraform/setup

# Create your terraform.tfvars (or use the example)
@"
project_name   = "your-project-name"
aws_region     = "us-east-1"
aws_account_id = "$(aws sts get-caller-identity --query Account --output text)"
cost_center    = "engineering"
budget_email   = "your.email@example.com"
"@ | Out-File -FilePath terraform.tfvars -Encoding UTF8

# Initialize and apply
terraform init
terraform validate
terraform plan -var-file=terraform.tfvars
terraform apply -var-file=terraform.tfvars
```

### Step 2: Configure GitHub Secrets

After Phase 4 completes, run the configuration script:

```powershell
# From project root
.\scripts\configure-github-secrets.ps1
```

This will automatically:
- Extract outputs from Terraform
- Create/update GitHub secrets
- Verify all required secrets are set

### Step 3: Verify Secrets

Go to your GitHub repository:
- Settings → Secrets and variables → Actions
- Verify all required secrets exist (see `docs/GITHUB_SECRETS.md` for the complete list)

### Step 4: Enable Workflows

Once Phase 4 is complete and secrets are configured:

1. **PR Checks** - Automatically run on every pull request
2. **Dev Deployment** - Automatically deploys when commits push to `main`
3. **Staging Deployment** - Manually triggered via GitHub Actions UI (optional)
4. **Prod Deployment** - Manually triggered via GitHub Actions UI

## Verification

The workflows now include a pre-check step that verifies the backend resources exist. If Phase 4 hasn't been completed, you'll see this error:

```
❌ ERROR: S3 bucket 'your-project-terraform-state-123456789' does not exist!
Please run Phase 4 (AWS Infrastructure Setup) first:
  cd terraform/setup
  terraform init
  terraform apply
```

## Common Issues

### Issue: "ResourceNotFoundException" during Terraform init

**Problem:** Phase 4 hasn't been completed yet  
**Solution:** Follow Step 1 above to create the backend resources

### Issue: GitHub Actions workflows fail immediately

**Problem:** Missing GitHub secrets  
**Solution:** Follow Step 2 to configure secrets from Terraform outputs

### Issue: ECR login fails

**Problem:** ECR repositories don't exist  
**Solution:** Complete Phase 4, which creates all ECR repositories

## What Happens If I Skip Phase 4?

The GitHub Actions workflows will fail with state lock errors because:
- Terraform can't store state without the S3 bucket
- Terraform can't acquire locks without the DynamoDB table
- Docker images can't be pushed without ECR repositories
- Services can't deploy without VPC and security groups

## Deployment Order

```
┌─────────────────────────────────────┐
│ Phase 4: AWS Infrastructure Setup  │ ← You are here (MUST DO FIRST)
│ (Manual - Run Locally)              │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│ Configure GitHub Secrets            │
│ (Run configure-github-secrets.ps1)  │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│ GitHub Actions CI/CD                │
│ (Automatic - Triggered by Git)      │
│                                     │
│ • Dev: On push to main (automatic)  │
│ • Staging: Manual trigger (optional)│
│ • Prod: Manual trigger              │
└─────────────────────────────────────┘
```

## Need Help?

See these additional guides:
- **[Phase 4: AWS Setup](.plan/phase4-aws-setup.md)** - Detailed setup guide
- **[Phase 4: Quick Start](.plan/phase4-quick-start.md)** - Fast reference
- **[GitHub Secrets](docs/GITHUB_SECRETS.md)** - Complete secrets reference
- **[IAM Setup](.plan/IAM_SETUP.md)** - Required AWS permissions

## Summary

✅ **DO THIS FIRST**: Run Phase 4 setup locally  
✅ **THEN**: Configure GitHub secrets  
✅ **FINALLY**: Push to `main` branch → Dev deploys automatically

**Deployment Flow:**
- **Push to `main`** → Dev environment (automatic)
- **Manual trigger** → Staging environment (optional)
- **Manual trigger** → Production environment

The workflows will automatically check for these prerequisites and provide clear error messages if something is missing.






