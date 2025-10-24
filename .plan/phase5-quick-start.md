# Phase 5: Quick Start Guide

This is a condensed version of Phase 5 for quick reference. **For detailed explanations, see `phase5-aws-deploy.md`**.

> ⚠️ **IMPORTANT UPDATE (Oct 2024)**
> - All 3 App Runner services (IdentityService, DataService, **ApiGateway**) are now properly configured
> - **All services** now include Cognito environment variables for JWT authentication
> - Port 8080 is used for all .NET 8 services (not port 80)
> - ECR authentication is properly configured for all services
> - Health checks are configured at `/health` endpoint
> - **Output aliases added** for GitHub Actions compatibility (`cloudfront_domain_name`, `frontend_s3_bucket_name`)

## TL;DR - Three-Step Process

### Step 1: Prepare Configuration

```powershell
# Auto-generate configuration from Phase 4 outputs
.\scripts\prepare-deploy.ps1

# This creates:
# - terraform/deploy/backend-config.tfvars
# - terraform/deploy/terraform.tfvars
```

### Step 2: Build and Push Docker Images

```powershell
# Build all backend services and push to ECR
.\scripts\build-and-push.ps1 -Environment dev
```

### Step 3: Deploy Infrastructure

```powershell
# Deploy RDS, App Runner, and S3/CloudFront
.\scripts\deploy.ps1 -Environment dev

# Or auto-approve (no confirmation)
.\scripts\deploy.ps1 -Environment dev -AutoApprove
```

---

## What Gets Created

### 🗄️ Database (RDS PostgreSQL)
- **Dev**: db.t3.micro, 20GB, single-AZ
- **Staging**: db.t3.small, 50GB, single-AZ
- **Prod**: db.t3.medium, 100GB, Multi-AZ

### 🚀 Microservices (App Runner)
- Identity Service (authentication)
- API Gateway (BFF pattern)
- Data Service (data operations)
- Auto-scaling enabled
- VPC connector for RDS access

### 🌐 Frontend (S3 + CloudFront)
- S3 bucket for static files
- CloudFront CDN distribution
- Automatic HTTPS
- SPA routing support

---

## Estimated Costs

**Dev Environment**: ~$30-40/month (or $0-10 with Free Tier)

Breakdown:
- RDS db.t3.micro: ~$12-15/month (Free Tier: 750 hours/month)
- App Runner (3 services): ~$15-25/month
- S3 + CloudFront: <$2/month (Free Tier)
- No NAT Gateway: $0 (saves $32-45/month)

---

## Common Issues

### ❌ "No backend-config.tfvars file"

**Solution:** Run `.\scripts\prepare-deploy.ps1` first

### ❌ "Image not found in ECR"

**Solution:** Run `.\scripts\build-and-push.ps1 -Environment dev` to push images

### ❌ "Terraform state lock"

**Solution:** Another deployment is in progress, or previous deployment failed

```powershell
# Force unlock (use with caution)
cd terraform/deploy
terraform force-unlock LOCK_ID
```

### ❌ "RDS creation taking too long"

**Normal:** RDS can take 10-15 minutes to create on first deployment

---

## Deployment Outputs

After successful deployment, you'll get:

```
identity_service_url = "https://xxx.us-east-1.awsapprunner.com"
api_gateway_url      = "https://yyy.us-east-1.awsapprunner.com"
data_service_url     = "https://zzz.us-east-1.awsapprunner.com"
frontend_url         = "https://ddd.cloudfront.net"
rds_endpoint         = "xxx.rds.amazonaws.com:5432"
```

---

## Next Steps

1. ✅ Infrastructure deployed
2. ➡️ Run database migrations
3. ➡️ Build and deploy frontend
4. ➡️ Test the application
5. ➡️ Proceed to [Phase 6: CI/CD Pipeline](phase6-cicd.md)

---

## 📚 Additional Documentation

| Document | Purpose |
|----------|---------|
| **[phase5-aws-deploy.md](phase5-aws-deploy.md)** | Full detailed guide with all Terraform configurations |
| **[COST_OPTIMIZATION.md](COST_OPTIMIZATION.md)** | Cost breakdown and optimization strategies |
| `terraform/deploy/modules/*/` | Individual module documentation |

---

**📌 This is a QUICK REFERENCE guide** - Read `phase5-aws-deploy.md` first if it's your first time!






