# IAM Setup Guide

This document explains how to set up the necessary IAM permissions for running this Terraform template.

## Overview

This template requires specific AWS permissions to create and manage infrastructure. We provide two approaches:

1. **Quick Setup**: Use AWS managed policies (easier, broader permissions)
2. **Secure Setup**: Use custom policy (recommended, least privilege)

## Estimated Monthly Cost

**IAM Users**: Free (no charge for IAM users or policies)

---

## Option 1: Quick Setup (AWS Managed Policies)

### For Development/Testing

Attach these AWS managed policies to your IAM user:

```
- AmazonEC2FullAccess
- AmazonRDSFullAccess
- AmazonS3FullAccess
- AmazonDynamoDBFullAccess
- AmazonECS_FullAccess (includes ECR)
- AWSAppRunnerFullAccess
- SecretsManagerReadWrite
- CloudWatchLogsFullAccess
- IAMFullAccess
- AWSBudgetsActionsWithAWSResourceControlAccess
- AWSCostExplorerReadOnlyAccess
```

### AWS Console Steps

1. Go to **IAM** → **Users** → Your username
2. Click **Add permissions** → **Attach policies directly**
3. Search and select each policy above
4. Click **Add permissions**

---

## Option 2: Secure Setup (Custom Policy)

### ⚠️ Important Limitation

The comprehensive IAM policy defined in `terraform/iam-policy.tf` **exceeds AWS's 6KB limit** for managed policies and **cannot be directly created**.

**Error you'll encounter:**
```
Error: creating IAM Policy: LimitExceeded: Cannot exceed quota for PolicySize: 6144
```

### Practical Approach (What Actually Works)

The `iam-policy.tf` serves as a **reference** for required permissions. Use one of these approaches instead:

#### Approach A: AWS Managed Policies (Fastest)

Use AWS-provided policies that cover broad services:

```powershell
# Core infrastructure
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/AmazonEC2FullAccess
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/AmazonS3FullAccess
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/AmazonRDSFullAccess
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/AmazonDynamoDBFullAccess

# Container & Compute
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/AmazonEC2ContainerRegistryFullAccess
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/AWSAppRunnerFullAccess

# Auth & Secrets
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/AmazonCognitoPowerUser
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/SecretsManagerReadWrite

# Monitoring & CDN
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/CloudWatchLogsFullAccess
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/CloudFrontFullAccess

# IAM (for App Runner roles)
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/IAMFullAccess

# Cost Management (inline policy - smaller)
aws iam put-user-policy --user-name YOUR_USERNAME --policy-name CostManagement --policy-document '{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Action": ["budgets:*", "ce:*"],
    "Resource": "*"
  }]
}'
```

#### Approach B: Split into Multiple Custom Policies

Create 2-3 smaller custom policies (each < 6KB):
1. **Policy 1**: Networking, Storage, Database (S3, EC2, RDS, DynamoDB)
2. **Policy 2**: Compute & Container (App Runner, ECR, IAM roles)
3. **Policy 3**: Supporting (Cognito, CloudWatch, Cost Management)

---

## Option 3: Create User via AWS CLI

### Create Policy and User Automatically

```powershell
# Set your desired username
$USERNAME = "terraform-user"

# Generate policy JSON from Terraform
cd terraform
terraform init
$POLICY_JSON = terraform output -raw policy_json
$POLICY_JSON | Out-File -FilePath policy-generated.json -Encoding UTF8
cd ..

# Create the IAM policy
aws iam create-policy `
  --policy-name TerraformTemplatePolicy `
  --policy-document file://terraform/policy-generated.json `
  --description "Permissions for Sri Template Terraform deployment"

# Get your AWS account ID
$ACCOUNT_ID = (aws sts get-caller-identity --query Account --output text)

# Create IAM user
aws iam create-user --user-name $USERNAME

# Attach the policy
aws iam attach-user-policy `
  --user-name $USERNAME `
  --policy-arn "arn:aws:iam::${ACCOUNT_ID}:policy/TerraformTemplatePolicy"

# Create access keys
aws iam create-access-key --user-name $USERNAME
```

**Save the access key output** - you'll need it to configure AWS CLI!

---

## Option 4: Create User via Terraform (Advanced)

### Create IAM User with Infrastructure

**File**: `terraform/setup-user/main.tf`

```hcl
terraform {
  required_version = ">= 1.5.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

variable "aws_region" {
  default = "us-east-1"
}

variable "username" {
  description = "IAM username for Terraform"
  default     = "terraform-user"
}

# Create IAM policy from JSON file
resource "aws_iam_policy" "terraform_template" {
  name        = "TerraformTemplatePolicy"
  description = "Permissions for Sri Template Terraform deployment"
  policy      = file("../setup/iam-policy.json")
}

# Create IAM user
resource "aws_iam_user" "terraform" {
  name = var.username
  path = "/terraform/"

  tags = {
    Purpose = "Terraform Infrastructure Management"
    Project = "sri-template"
  }
}

# Attach policy to user
resource "aws_iam_user_policy_attachment" "terraform" {
  user       = aws_iam_user.terraform.name
  policy_arn = aws_iam_policy.terraform_template.arn
}

# Create access key (output is sensitive)
resource "aws_iam_access_key" "terraform" {
  user = aws_iam_user.terraform.name
}

# Outputs
output "username" {
  value = aws_iam_user.terraform.name
}

output "access_key_id" {
  value = aws_iam_access_key.terraform.id
}

output "secret_access_key" {
  value     = aws_iam_access_key.terraform.secret
  sensitive = true
}

output "instructions" {
  value = <<-EOT

  ✅ IAM user created successfully!

  Configure AWS CLI with these credentials:

  aws configure

  AWS Access Key ID: ${aws_iam_access_key.terraform.id}
  AWS Secret Access Key: (shown above - copy it now!)
  Default region name: us-east-1
  Default output format: json

  EOT
}
```

**To use:**

```powershell
# Must be run with admin credentials first
cd terraform/setup-user
terraform init
terraform apply

# Get the secret key (shown only once!)
terraform output secret_access_key
```

---

## What This Policy Allows

The custom policy (generated from `terraform/iam-policy.tf`) grants permissions for:

### Phase 4 (Setup):

- ✅ S3 bucket creation (Terraform state)
- ✅ DynamoDB table (state locking)
- ✅ VPC, Subnets, Internet Gateway, Route Tables
- ✅ Security Groups
- ✅ ECR repositories (4 repos)
- ✅ Cognito User Pool
- ✅ CloudWatch Log Groups
- ✅ Budget alerts
- ✅ Cost anomaly detection

### Phase 5 (Deployment):

- ✅ RDS PostgreSQL instance
- ✅ App Runner services
- ✅ Secrets Manager
- ✅ IAM roles for App Runner
- ✅ CloudFront distribution
- ✅ S3 frontend bucket

### What It Does NOT Allow

❌ EC2 instances (not needed - we use App Runner)
❌ Lambda functions (not needed)
❌ Route53 (DNS management)
❌ Certificate Manager (ACM)
❌ Delete S3 buckets with content (safety)
❌ Modify IAM users or groups

---

## Security Best Practices

### 1. Use MFA (Multi-Factor Authentication)

```powershell
# Enable MFA for your user
aws iam enable-mfa-device `
  --user-name terraform-user `
  --serial-number arn:aws:iam::ACCOUNT_ID:mfa/terraform-user `
  --authentication-code-1 123456 `
  --authentication-code-2 789012
```

### 2. Rotate Access Keys Regularly

```powershell
# Create new key
aws iam create-access-key --user-name terraform-user

# Delete old key (after updating config)
aws iam delete-access-key --user-name terraform-user --access-key-id OLD_KEY_ID
```

### 3. Use AWS Vault for Credential Management

```powershell
# Install aws-vault
choco install aws-vault

# Add credentials securely
aws-vault add terraform-user

# Use with Terraform
aws-vault exec terraform-user -- terraform apply
```

### 4. Limit by IP Address (Optional)

Add IP restriction to the policy:

```json
{
  "Condition": {
    "IpAddress": {
      "aws:SourceIp": ["YOUR.IP.ADDRESS/32"]
    }
  }
}
```

---

## Troubleshooting

### "Access Denied" Error

1. Check policy is attached: `aws iam list-attached-user-policies --user-name YOUR_USER`
2. Verify credentials: `aws sts get-caller-identity`
3. Check region matches: `us-east-1`

### "Policy Too Large" Error

The policy is under the 6,144 character limit. If you hit limits:

- Split into multiple policies
- Use AWS managed policies where possible

### Testing Permissions

```powershell
# Test S3 access
aws s3 ls

# Test EC2 access
aws ec2 describe-availability-zones

# Test Cognito access
aws cognito-idp list-user-pools --max-results 1
```

---

## For Template Users

### Quick Start

1. **Create IAM policy** (requires admin access):

   ```powershell
   cd terraform
   terraform init
   terraform apply -target=aws_iam_policy.terraform_template
   ```

2. **Attach to your user**:

   ```powershell
   $POLICY_ARN = terraform output -raw policy_arn
   aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn $POLICY_ARN
   ```

3. **Run infrastructure setup** (with regular user credentials):
   ```powershell
   cd setup
   terraform init
   terraform apply -var-file=terraform.tfvars
   ```

### Minimum Required

You **MUST** have at least:

- `sts:GetCallerIdentity` (to get account ID)
- `ec2:DescribeAvailabilityZones` (to list AZs)
- S3, DynamoDB, EC2, ECR, Cognito permissions

---

## Cost Optimization

**IAM itself is free**, but proper permissions help avoid:

- ❌ Accidental resource creation
- ❌ Overly permissive access
- ✅ Clear audit trail
- ✅ Team member access control

---

## Next Steps

After setting up IAM:

1. ✅ Verify permissions: `aws sts get-caller-identity`
2. ✅ Run prerequisites: `.\scripts\check-prerequisites.ps1`
3. ✅ Initialize Terraform: `terraform init`
4. ✅ Apply infrastructure: `terraform apply`

---

_Last updated: October 2025_





