# Phase 4: Quick Start Guide

This is a condensed version of Phase 4 for quick reference. **For detailed explanations, see `phase4-aws-setup.md`**.

## TL;DR - Two-Step Process

### Step 1: Setup IAM Permissions (Admin - ONE TIME)

⚠️ **Note:** The Terraform policy approach doesn't work (exceeds AWS 6KB limit). Use AWS managed policies instead:

```powershell
# Replace YOUR_USERNAME with your IAM username

# Quick approach: Attach AWS managed policies
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/AmazonEC2FullAccess
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/AmazonS3FullAccess
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/AmazonRDSFullAccess
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/AmazonDynamoDBFullAccess
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/AmazonEC2ContainerRegistryFullAccess
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/AWSAppRunnerFullAccess
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/AmazonCognitoPowerUser
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/SecretsManagerReadWrite
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/CloudWatchLogsFullAccess
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/CloudFrontFullAccess
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/IAMFullAccess

# Cost Management (inline policy)
aws iam put-user-policy --user-name YOUR_USERNAME --policy-name CostManagement --policy-document '{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Action": ["budgets:*", "ce:*"],
    "Resource": "*"
  }]
}'
```

**For detailed IAM setup options, see:** `.plan/IAM_SETUP.md`

### Step 2: Create Infrastructure

```powershell
# From project root
cd terraform/setup

# Create terraform.tfvars
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

---

## Common Issues

### ❌ "Access Denied - ec2:DescribeAvailabilityZones"

**Problem:** Missing IAM permissions

**Solution:** Complete Step 1 above, or temporarily add `AmazonEC2FullAccess` managed policy

```powershell
aws iam attach-user-policy `
  --user-name YOUR_USERNAME `
  --policy-arn arn:aws:iam::aws:policy/AmazonEC2FullAccess
```

### ❌ "Duplicate resource aws_s3_bucket"

**Problem:** S3 bucket resources in wrong file

**Solution:** Check that `terraform/setup/dynamodb.tf` only contains DynamoDB table, not S3 resources

### ❌ "Policy Too Large"

**Problem:** IAM policy exceeds size limit

**Solution:** The policy is already optimized. If you added custom permissions, split into multiple policies.

---

## What Gets Created

### Free/Minimal Cost:

- VPC with 2 public subnets (no NAT Gateway!)
- Internet Gateway
- Route Tables
- Security Groups (App Runner, RDS)
- S3 bucket for Terraform state
- DynamoDB table for state locking
- 4 ECR repositories
- Cognito User Pool
- CloudWatch Log Groups (7-day retention)
- Budget alerts
- Cost anomaly detection

### Monthly Cost (~$0-10 with Free Tier):

All resources above are mostly free or very low cost during development.

---

## Next Steps

1. ✅ IAM policy created and attached
2. ✅ Infrastructure deployed
3. ➡️ Proceed to [Phase 5: AWS App Deployment](phase5-aws-deploy.md)

---

## 📚 Additional Documentation

| Document | Purpose |
|----------|---------|
| **[phase4-aws-setup.md](phase4-aws-setup.md)** | Full detailed guide with all options and explanations |
| **[IAM_SETUP.md](IAM_SETUP.md)** | Comprehensive IAM permission setup guide |
| **[COST_OPTIMIZATION.md](COST_OPTIMIZATION.md)** | Detailed cost breakdown and optimization strategies |
| `terraform/IAM_POLICY_README.md` | IAM policy technical documentation |

---

**📌 This is a QUICK REFERENCE guide** - Read `phase4-aws-setup.md` first if it's your first time!





