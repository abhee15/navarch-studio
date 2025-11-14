# Phase 4: AWS Infrastructure Setup

> 📖 **Quick Start?** If you've done this before, see [`phase4-quick-start.md`](phase4-quick-start.md) for a condensed version.  
> 📚 **Detailed Guide:** This document provides comprehensive explanations, multiple options, and troubleshooting.

## Goal

Create one-time AWS infrastructure setup including S3 backend for Terraform state, ECR repositories, VPC networking, and cost monitoring. This phase sets up the foundation for all future deployments.

**Cost-Optimized Design**: This template is designed to minimize AWS costs by using only public subnets (no NAT Gateway), AWS Free Tier eligible services, and minimal resource sizes suitable for learning and development.

## Prerequisites

- Phase 3 completed (database migrations working)
- AWS CLI installed and configured
- Terraform installed
- AWS account with appropriate permissions
- Understanding of AWS services
- **IAM permissions** (see Step 0 below for setup)

## Estimated Monthly Costs (Dev Environment)

- **RDS db.t3.micro** (20GB): ~$12-15/month (Free Tier: 750 hours/month for 12 months)
- **App Runner** (3 services @ 256MB/0.25vCPU): ~$15-25/month (depends on usage)
- **S3 Storage**: <$1/month (Free Tier: 5GB)
- **CloudFront**: <$1/month (Free Tier: 50GB data transfer)
- **ECR**: <$1/month (Free Tier: 500MB)
- **No NAT Gateway**: $0 (saves $32-45/month)
- **Total Estimated**: ~$30-40/month (or ~$0-10/month with Free Tier)

## Deliverables Checklist

### 0. IAM Policy Setup (ONE TIME - Admin Task)

⚠️ **IMPORTANT:** Before running any Terraform in this phase, you need proper IAM permissions.

#### Option A: Use AWS Managed Policies (Recommended for Quick Start)

⚠️ **Note**: The comprehensive policy in `terraform/iam-policy.tf` exceeds AWS's 6KB limit for managed policies. Use AWS managed policies + inline policies instead.

**Setup Steps:**

```powershell
# Replace YOUR_USERNAME with your IAM username

# 1. Core AWS Services
aws iam attach-user-policy `
  --user-name YOUR_USERNAME `
  --policy-arn arn:aws:iam::aws:policy/AmazonEC2FullAccess

aws iam attach-user-policy `
  --user-name YOUR_USERNAME `
  --policy-arn arn:aws:iam::aws:policy/AmazonS3FullAccess

aws iam attach-user-policy `
  --user-name YOUR_USERNAME `
  --policy-arn arn:aws:iam::aws:policy/AmazonRDSFullAccess

aws iam attach-user-policy `
  --user-name YOUR_USERNAME `
  --policy-arn arn:aws:iam::aws:policy/AmazonDynamoDBFullAccess

# 2. Container & App Services
aws iam attach-user-policy `
  --user-name YOUR_USERNAME `
  --policy-arn arn:aws:iam::aws:policy/AmazonEC2ContainerRegistryFullAccess

aws iam attach-user-policy `
  --user-name YOUR_USERNAME `
  --policy-arn arn:aws:iam::aws:policy/AWSAppRunnerFullAccess

# 3. Authentication & Secrets
aws iam attach-user-policy `
  --user-name YOUR_USERNAME `
  --policy-arn arn:aws:iam::aws:policy/AmazonCognitoPowerUser

aws iam attach-user-policy `
  --user-name YOUR_USERNAME `
  --policy-arn arn:aws:iam::aws:policy/SecretsManagerReadWrite

# 4. Logging & Monitoring
aws iam attach-user-policy `
  --user-name YOUR_USERNAME `
  --policy-arn arn:aws:iam::aws:policy/CloudWatchLogsFullAccess

aws iam attach-user-policy `
  --user-name YOUR_USERNAME `
  --policy-arn arn:aws:iam::aws:policy/CloudFrontFullAccess

# 5. IAM for creating App Runner roles
aws iam attach-user-policy `
  --user-name YOUR_USERNAME `
  --policy-arn arn:aws:iam::aws:policy/IAMFullAccess

# 6. Cost Management (inline policy - smaller)
aws iam put-user-policy --user-name YOUR_USERNAME --policy-name CostManagement --policy-document '{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Action": ["budgets:*", "ce:*"],
    "Resource": "*"
  }]
}'

# Verify all attached policies
aws iam list-attached-user-policies --user-name YOUR_USERNAME
aws iam list-user-policies --user-name YOUR_USERNAME
```

#### Option B: Use AWS Console

1. Go to `terraform/` directory
2. Run `terraform init`
3. Export policy: `terraform output -raw policy_json | Out-File -FilePath policy-generated.json`
4. In AWS Console: IAM → Policies → Create policy
5. Paste JSON from `policy-generated.json`
6. Name it: `TerraformTemplatePolicy`
7. Attach to your user: IAM → Users → Your user → Add permissions

#### Option C: Quick Fix (For Testing)

Temporarily attach these AWS managed policies to your user:

```powershell
aws iam attach-user-policy `
  --user-name YOUR_USERNAME `
  --policy-arn arn:aws:iam::aws:policy/AmazonEC2FullAccess

aws iam attach-user-policy `
  --user-name YOUR_USERNAME `
  --policy-arn arn:aws:iam::aws:policy/AmazonS3FullAccess

# Add others as needed...
```

**See** `.plan/IAM_SETUP.md` and `terraform/IAM_POLICY_README.md` for detailed instructions.

---

### 1. Prerequisites Checker

#### Create Prerequisites Script

**File**: `scripts/check-prerequisites.ps1`

```powershell
# Check prerequisites for AWS setup
Write-Host "Checking prerequisites..."

$errors = @()

# Check AWS CLI
try {
    $awsVersion = aws --version 2>$null
    if ($awsVersion) {
        Write-Host "✓ AWS CLI: $awsVersion"
    } else {
        $errors += "AWS CLI not found"
    }
} catch {
    $errors += "AWS CLI not found"
}

# Check Terraform
try {
    $terraformVersion = terraform --version 2>$null
    if ($terraformVersion) {
        Write-Host "✓ Terraform: $terraformVersion"
    } else {
        $errors += "Terraform not found"
    }
} catch {
    $errors += "Terraform not found"
}

# Check Docker
try {
    $dockerVersion = docker --version 2>$null
    if ($dockerVersion) {
        Write-Host "✓ Docker: $dockerVersion"
    } else {
        $errors += "Docker not found"
    }
} catch {
    $errors += "Docker not found"
}

# Check AWS credentials
try {
    $awsIdentity = aws sts get-caller-identity 2>$null
    if ($awsIdentity) {
        $identity = $awsIdentity | ConvertFrom-Json
        Write-Host "✓ AWS Account: $($identity.Account)"
        Write-Host "✓ AWS User: $($identity.Arn)"
    } else {
        $errors += "AWS credentials not configured"
    }
} catch {
    $errors += "AWS credentials not configured"
}

if ($errors.Count -gt 0) {
    Write-Host "`n❌ Prerequisites check failed:"
    $errors | ForEach-Object { Write-Host "  - $_" }
    exit 1
} else {
    Write-Host "`n✓ All prerequisites met!"
}
```

### 2. Terraform Setup Module

#### Create Setup Directory Structure

```bash
mkdir -p terraform/setup
cd terraform/setup
```

#### Main Configuration

**File**: `terraform/setup/main.tf`

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

  default_tags {
    tags = {
      Project     = var.project_name
      Environment = "setup"
      ManagedBy   = "Terraform"
      CostCenter  = var.cost_center
    }
  }
}

# Data sources
data "aws_caller_identity" "current" {}
data "aws_availability_zones" "available" {
  state = "available"
}

# Note: Common tags are defined in provider default_tags block above
# No need for locals.common_tags
```

#### Variables

**File**: `terraform/setup/variables.tf`

```hcl
variable "project_name" {
  description = "Project name used for resource naming"
  type        = string
  default     = "sri-subscription"

  validation {
    condition     = can(regex("^[a-z0-9-]+$", var.project_name))
    error_message = "Project name must contain only lowercase letters, numbers, and hyphens."
  }
}

variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "us-east-1"
}

variable "aws_account_id" {
  description = "AWS account ID"
  type        = string
  sensitive   = true
}

variable "cost_center" {
  description = "Cost center for billing"
  type        = string
  default     = "engineering"
}

variable "environment" {
  description = "Environment name"
  type        = string
  default     = "setup"
}

variable "budget_email" {
  description = "Email address for budget and cost anomaly alerts"
  type        = string
}
```

#### S3 Backend Configuration

**File**: `terraform/setup/s3-backend.tf`

```hcl
# S3 bucket for Terraform state
resource "aws_s3_bucket" "terraform_state" {
  bucket = "${var.project_name}-terraform-state-${data.aws_caller_identity.current.account_id}"

  tags = {
    Name = "${var.project_name}-terraform-state"
  }
}

# S3 bucket versioning
resource "aws_s3_bucket_versioning" "terraform_state" {
  bucket = aws_s3_bucket.terraform_state.id

  versioning_configuration {
    status = "Enabled"
  }
}

# S3 bucket encryption
resource "aws_s3_bucket_server_side_encryption_configuration" "terraform_state" {
  bucket = aws_s3_bucket.terraform_state.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

# S3 bucket lifecycle configuration
resource "aws_s3_bucket_lifecycle_configuration" "terraform_state" {
  bucket = aws_s3_bucket.terraform_state.id

  rule {
    id     = "delete_old_versions"
    status = "Enabled"

    noncurrent_version_expiration {
      noncurrent_days = 30
    }
  }
}

# S3 bucket public access block
resource "aws_s3_bucket_public_access_block" "terraform_state" {
  bucket = aws_s3_bucket.terraform_state.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}
```

#### DynamoDB for State Locking

**File**: `terraform/setup/dynamodb.tf`

```hcl
# DynamoDB table for Terraform state locking
resource "aws_dynamodb_table" "terraform_locks" {
  name         = "${var.project_name}-terraform-locks"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "LockID"

  attribute {
    name = "LockID"
    type = "S"
  }

  tags = {
    Name = "${var.project_name}-terraform-locks"
  }
}
```

#### ECR Repositories

**File**: `terraform/setup/ecr.tf`

```hcl
# ECR repositories for container images
resource "aws_ecr_repository" "identity_service" {
  name                 = "${var.project_name}-identity-service"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }

  tags = {
    Name = "${var.project_name}-identity-service"
  }
}

resource "aws_ecr_repository" "api_gateway" {
  name                 = "${var.project_name}-api-gateway"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }

  tags = {
    Name = "${var.project_name}-api-gateway"
  }
}

resource "aws_ecr_repository" "data_service" {
  name                 = "${var.project_name}-data-service"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }

  tags = {
    Name = "${var.project_name}-data-service"
  }
}

resource "aws_ecr_repository" "frontend" {
  name                 = "${var.project_name}-frontend"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }

  tags = {
    Name = "${var.project_name}-frontend"
  }
}

# ECR lifecycle policies
resource "aws_ecr_lifecycle_policy" "identity_service" {
  repository = aws_ecr_repository.identity_service.name

  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Keep last 10 images"
        selection = {
          tagStatus     = "tagged"
          tagPrefixList = ["v"]
          countType     = "imageCountMoreThan"
          countNumber   = 10
        }
        action = {
          type = "expire"
        }
      }
    ]
  })
}

resource "aws_ecr_lifecycle_policy" "api_gateway" {
  repository = aws_ecr_repository.api_gateway.name

  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Keep last 10 images"
        selection = {
          tagStatus     = "tagged"
          tagPrefixList = ["v"]
          countType     = "imageCountMoreThan"
          countNumber   = 10
        }
        action = {
          type = "expire"
        }
      }
    ]
  })
}

resource "aws_ecr_lifecycle_policy" "data_service" {
  repository = aws_ecr_repository.data_service.name

  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Keep last 10 images"
        selection = {
          tagStatus     = "tagged"
          tagPrefixList = ["v"]
          countType     = "imageCountMoreThan"
          countNumber   = 10
        }
        action = {
          type = "expire"
        }
      }
    ]
  })
}

resource "aws_ecr_lifecycle_policy" "frontend" {
  repository = aws_ecr_repository.frontend.name

  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Keep last 10 images"
        selection = {
          tagStatus     = "tagged"
          tagPrefixList = ["v"]
          countType     = "imageCountMoreThan"
          countNumber   = 10
        }
        action = {
          type = "expire"
        }
      }
    ]
  })
}
```

#### VPC and Networking

**File**: `terraform/setup/networking.tf`

**Cost Optimization**: This configuration uses **only public subnets** to avoid NAT Gateway costs (~$32-45/month). RDS and App Runner will use public subnets with strict security groups for cost-effective development and testing.

```hcl
# VPC
resource "aws_vpc" "main" {
  cidr_block           = "10.0.0.0/16"
  enable_dns_hostnames = true
  enable_dns_support   = true

  tags = {
    Name = "${var.project_name}-vpc"
  }
}

# Internet Gateway
resource "aws_internet_gateway" "main" {
  vpc_id = aws_vpc.main.id

  tags = {
    Name = "${var.project_name}-igw"
  }
}

# Public Subnets (using multiple AZs for RDS multi-AZ requirement)
resource "aws_subnet" "public" {
  count = 2

  vpc_id                  = aws_vpc.main.id
  cidr_block              = "10.0.${count.index + 1}.0/24"
  availability_zone       = data.aws_availability_zones.available.names[count.index]
  map_public_ip_on_launch = true

  tags = {
    Name = "${var.project_name}-public-subnet-${count.index + 1}"
    Type = "Public"
  }
}

# Route Table for Public Subnets
resource "aws_route_table" "public" {
  vpc_id = aws_vpc.main.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.main.id
  }

  tags = {
    Name = "${var.project_name}-public-rt"
  }
}

# Route Table Associations
resource "aws_route_table_association" "public" {
  count = 2

  subnet_id      = aws_subnet.public[count.index].id
  route_table_id = aws_route_table.public.id
}
```

#### Security Groups

**File**: `terraform/setup/security-groups.tf`

**Security Note**: Even though we're using public subnets, security groups provide strong network isolation. RDS only accepts connections from App Runner services, not from the public internet.

```hcl
# Security group for App Runner services
resource "aws_security_group" "app_runner" {
  name_prefix = "${var.project_name}-app-runner-"
  vpc_id      = aws_vpc.main.id
  description = "Security group for App Runner services"

  # App Runner manages ingress rules automatically
  # We only define egress rules

  egress {
    description = "Allow all outbound traffic"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "${var.project_name}-app-runner-sg"
  }
}

# Security group for RDS
resource "aws_security_group" "rds" {
  name_prefix = "${var.project_name}-rds-"
  vpc_id      = aws_vpc.main.id
  description = "Security group for RDS - only allows App Runner access"

  ingress {
    description     = "PostgreSQL from App Runner"
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.app_runner.id]
  }

  ingress {
    description = "PostgreSQL from VPC (for migrations)"
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = [aws_vpc.main.cidr_block]
  }

  egress {
    description = "Allow all outbound traffic"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "${var.project_name}-rds-sg"
  }
}
```

#### Cognito Setup

**File**: `terraform/setup/cognito.tf`

```hcl
# Cognito User Pool
resource "aws_cognito_user_pool" "main" {
  name = "${var.project_name}-user-pool"

  password_policy {
    minimum_length    = 8
    require_lowercase = true
    require_uppercase = true
    require_numbers   = true
    require_symbols   = true
  }

  username_attributes = ["email"]
  auto_verified_attributes = ["email"]

  account_recovery_setting {
    recovery_mechanism {
      name     = "verified_email"
      priority = 1
    }
  }

  tags = {
    Name = "${var.project_name}-user-pool"
  }
}

# Cognito User Pool Client
resource "aws_cognito_user_pool_client" "main" {
  name         = "${var.project_name}-client"
  user_pool_id = aws_cognito_user_pool.main.id

  generate_secret                      = false
  prevent_user_existence_errors        = "ENABLED"
  enable_token_revocation             = true
  enable_propagate_additional_user_context_data = false

  token_validity_units {
    access_token  = "hours"
    id_token      = "hours"
    refresh_token = "days"
  }

  access_token_validity  = 1
  id_token_validity      = 1
  refresh_token_validity = 30

  explicit_auth_flows = [
    "ALLOW_USER_SRP_AUTH",
    "ALLOW_REFRESH_TOKEN_AUTH"
  ]
}

# Cognito User Pool Domain
resource "aws_cognito_user_pool_domain" "main" {
  domain       = "${var.project_name}-${random_string.domain_suffix.result}"
  user_pool_id = aws_cognito_user_pool.main.id
}

# Random string for domain uniqueness
resource "random_string" "domain_suffix" {
  length  = 8
  special = false
  upper   = false
}
```

#### CloudWatch Log Groups

**File**: `terraform/setup/cloudwatch.tf`

**Cost Optimization**: Using 7-day log retention for dev environments (can be increased for production).

```hcl
# CloudWatch log groups for services
resource "aws_cloudwatch_log_group" "identity_service" {
  name              = "/aws/apprunner/${var.project_name}-identity-service"
  retention_in_days = 7  # Reduced from 14 to save costs

  tags = {
    Name = "${var.project_name}-identity-service-logs"
  }
}

resource "aws_cloudwatch_log_group" "api_gateway" {
  name              = "/aws/apprunner/${var.project_name}-api-gateway"
  retention_in_days = 7  # Reduced from 14 to save costs

  tags = {
    Name = "${var.project_name}-api-gateway-logs"
  }
}

resource "aws_cloudwatch_log_group" "data_service" {
  name              = "/aws/apprunner/${var.project_name}-data-service"
  retention_in_days = 7  # Reduced from 14 to save costs

  tags = {
    Name = "${var.project_name}-data-service-logs"
  }
}
```

#### Cost Monitoring

**File**: `terraform/setup/cost-monitoring.tf`

```hcl
# AWS Budget for cost monitoring
resource "aws_budgets_budget" "monthly" {
  name         = "${var.project_name}-monthly-budget"
  budget_type  = "COST"
  limit_amount = "100"
  limit_unit   = "USD"
  time_unit    = "MONTHLY"

  notification {
    comparison_operator        = "GREATER_THAN"
    threshold                  = 80
    threshold_type             = "PERCENTAGE"
    notification_type          = "ACTUAL"
    subscriber_email_addresses = [var.budget_email]
  }

  notification {
    comparison_operator        = "GREATER_THAN"
    threshold                  = 100
    threshold_type             = "PERCENTAGE"
    notification_type          = "FORECASTED"
    subscriber_email_addresses = [var.budget_email]
  }

  tags = {
    Name = "${var.project_name}-monthly-budget"
  }
}

# Cost anomaly monitor (must be created before detector)
resource "aws_ce_anomaly_monitor" "main" {
  name              = "${var.project_name}-anomaly-monitor"
  monitor_type      = "DIMENSIONAL"
  monitor_dimension = "SERVICE"

  tags = {
    Name = "${var.project_name}-anomaly-monitor"
  }
}

# Cost anomaly detector
resource "aws_ce_anomaly_subscription" "main" {
  name      = "${var.project_name}-anomaly-subscription"
  frequency = "DAILY"

  monitor_arn_list = [
    aws_ce_anomaly_monitor.main.arn
  ]

  subscriber {
    type    = "EMAIL"
    address = var.budget_email
  }

  threshold_expression {
    dimension {
      key           = "ANOMALY_TOTAL_IMPACT_ABSOLUTE"
      values        = ["10"]
      match_options = ["GREATER_THAN_OR_EQUAL"]
    }
  }

  tags = {
    Name = "${var.project_name}-anomaly-subscription"
  }
}
```

#### Outputs

**File**: `terraform/setup/outputs.tf`

```hcl
output "s3_bucket_name" {
  description = "S3 bucket name for Terraform state"
  value       = aws_s3_bucket.terraform_state.bucket
}

output "dynamodb_table_name" {
  description = "DynamoDB table name for Terraform state locking"
  value       = aws_dynamodb_table.terraform_locks.name
}

output "ecr_repository_urls" {
  description = "ECR repository URLs"
  value = {
    identity_service = aws_ecr_repository.identity_service.repository_url
    api_gateway      = aws_ecr_repository.api_gateway.repository_url
    data_service     = aws_ecr_repository.data_service.repository_url
    frontend         = aws_ecr_repository.frontend.repository_url
  }
}

output "vpc_id" {
  description = "VPC ID"
  value       = aws_vpc.main.id
}

output "public_subnet_ids" {
  description = "Public subnet IDs"
  value       = aws_subnet.public[*].id
}

output "app_runner_security_group_id" {
  description = "App Runner security group ID"
  value       = aws_security_group.app_runner.id
}

output "rds_security_group_id" {
  description = "RDS security group ID"
  value       = aws_security_group.rds.id
}

output "cognito_user_pool_id" {
  description = "Cognito User Pool ID"
  value       = aws_cognito_user_pool.main.id
}

output "cognito_user_pool_client_id" {
  description = "Cognito User Pool Client ID"
  value       = aws_cognito_user_pool_client.main.id
}

output "cognito_domain" {
  description = "Cognito domain"
  value       = aws_cognito_user_pool_domain.main.domain
}
```

### 3. Verify IAM Permissions

Before running the main setup, verify you have the necessary permissions:

```powershell
# Test basic permissions
aws sts get-caller-identity
aws ec2 describe-availability-zones --region us-east-1
aws s3 ls

# If any fail, go back to Step 0 and setup IAM policy
```

---

### 4. Setup Automation Script

#### Main Setup Script

**File**: `scripts/setup.ps1`

```powershell
# AWS Infrastructure Setup Script
param(
    [string]$ProjectName = "sri-subscription",
    [string]$AwsRegion = "us-east-1",
    [string]$AwsAccountId = "",
    [string]$BudgetEmail = "",
    [string]$CostCenter = "engineering"
)

Write-Host "🚀 Setting up AWS infrastructure for $ProjectName" -ForegroundColor Green

# Check prerequisites
Write-Host "`n📋 Checking prerequisites..." -ForegroundColor Yellow
& "$PSScriptRoot/check-prerequisites.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Prerequisites check failed" -ForegroundColor Red
    exit 1
}

# Get AWS account ID if not provided
if ([string]::IsNullOrEmpty($AwsAccountId)) {
    Write-Host "`n🔍 Getting AWS account ID..." -ForegroundColor Yellow
    $AwsAccountId = (aws sts get-caller-identity --query Account --output text)
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to get AWS account ID" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ AWS Account ID: $AwsAccountId" -ForegroundColor Green
}

# Get budget email if not provided
if ([string]::IsNullOrEmpty($BudgetEmail)) {
    $BudgetEmail = Read-Host "Enter email for budget alerts"
    if ([string]::IsNullOrEmpty($BudgetEmail)) {
        Write-Host "❌ Budget email is required" -ForegroundColor Red
        exit 1
    }
}

# Create terraform.tfvars
Write-Host "`n📝 Creating terraform.tfvars..." -ForegroundColor Yellow
$tfvarsContent = @"
project_name = "$ProjectName"
aws_region = "$AwsRegion"
aws_account_id = "$AwsAccountId"
cost_center = "$CostCenter"
budget_email = "$BudgetEmail"
"@

$tfvarsContent | Out-File -FilePath "terraform/setup/terraform.tfvars" -Encoding UTF8
Write-Host "✓ Created terraform.tfvars" -ForegroundColor Green

# Initialize Terraform
Write-Host "`n🔧 Initializing Terraform..." -ForegroundColor Yellow
Set-Location "terraform/setup"
terraform init
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Terraform init failed" -ForegroundColor Red
    exit 1
}

# Plan Terraform
Write-Host "`n📋 Planning Terraform..." -ForegroundColor Yellow
terraform plan -var-file="terraform.tfvars"
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Terraform plan failed" -ForegroundColor Red
    exit 1
}

# Apply Terraform
Write-Host "`n🚀 Applying Terraform..." -ForegroundColor Yellow
terraform apply -var-file="terraform.tfvars" -auto-approve
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Terraform apply failed" -ForegroundColor Red
    exit 1
}

# Get outputs
Write-Host "`n📤 Getting Terraform outputs..." -ForegroundColor Yellow
$outputs = terraform output -json | ConvertFrom-Json

# Create .env files for services
Write-Host "`n📝 Creating .env files..." -ForegroundColor Yellow

# Frontend .env
$frontendEnv = @"
VITE_API_URL=https://api-gateway.sri-subscription.com
VITE_COGNITO_USER_POOL_ID=$($outputs.cognito_user_pool_id.value)
VITE_COGNITO_CLIENT_ID=$($outputs.cognito_user_pool_client_id.value)
VITE_COGNITO_DOMAIN=$($outputs.cognito_domain.value)
"@
$frontendEnv | Out-File -FilePath "../frontend/.env" -Encoding UTF8

# Identity Service .env
$identityEnv = @"
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=sri-subscription_dev;Username=postgres;Password=postgres
ASPNETCORE_ENVIRONMENT=Development
"@
$identityEnv | Out-File -FilePath "../backend/IdentityService/.env" -Encoding UTF8

# API Gateway .env
$gatewayEnv = @"
Services__IdentityService=http://localhost:5001
Services__DataService=http://localhost:5003
ASPNETCORE_ENVIRONMENT=Development
"@
$gatewayEnv | Out-File -FilePath "../backend/ApiGateway/.env" -Encoding UTF8

# Data Service .env
$dataEnv = @"
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=sri-subscription_dev;Username=postgres;Password=postgres
ASPNETCORE_ENVIRONMENT=Development
"@
$dataEnv | Out-File -FilePath "../backend/DataService/.env" -Encoding UTF8

Write-Host "✓ Created .env files" -ForegroundColor Green

# Generate GitHub secrets instructions
Write-Host "`n📋 GitHub Secrets Setup Instructions:" -ForegroundColor Yellow
Write-Host "Add these secrets to your GitHub repository:"
Write-Host ""
Write-Host "AWS_ACCESS_KEY_ID=<your-access-key>"
Write-Host "AWS_SECRET_ACCESS_KEY=<your-secret-key>"
Write-Host "AWS_REGION=$AwsRegion"
Write-Host "ECR_IDENTITY_SERVICE_URL=$($outputs.ecr_repository_urls.value.identity_service)"
Write-Host "ECR_API_GATEWAY_URL=$($outputs.ecr_repository_urls.value.api_gateway)"
Write-Host "ECR_DATA_SERVICE_URL=$($outputs.ecr_repository_urls.value.data_service)"
Write-Host "ECR_FRONTEND_URL=$($outputs.ecr_repository_urls.value.frontend)"
Write-Host "S3_BUCKET_NAME=$($outputs.s3_bucket_name.value)"
Write-Host "DYNAMODB_TABLE_NAME=$($outputs.dynamodb_table_name.value)"
Write-Host "VPC_ID=$($outputs.vpc_id.value)"
Write-Host "PUBLIC_SUBNET_IDS=$($outputs.public_subnet_ids.value -join ',')"
Write-Host "APP_RUNNER_SECURITY_GROUP_ID=$($outputs.app_runner_security_group_id.value)"
Write-Host "RDS_SECURITY_GROUP_ID=$($outputs.rds_security_group_id.value)"
Write-Host "COGNITO_USER_POOL_ID=$($outputs.cognito_user_pool_id.value)"
Write-Host "COGNITO_USER_POOL_CLIENT_ID=$($outputs.cognito_user_pool_client_id.value)"
Write-Host "COGNITO_DOMAIN=$($outputs.cognito_domain.value)"

Write-Host "`n✅ AWS infrastructure setup completed!" -ForegroundColor Green
Write-Host "Next steps:"
Write-Host "1. Add GitHub secrets listed above"
Write-Host "2. Proceed to Phase 5: AWS App Deployment"
```

### 5. Validation Script

**File**: `scripts/validate-setup.ps1`

```powershell
# Validate AWS setup
Write-Host "🔍 Validating AWS setup..." -ForegroundColor Yellow

# Get AWS account ID
$AwsAccountId = (aws sts get-caller-identity --query Account --output text)

# Check S3 bucket
Write-Host "Checking S3 bucket..."
$bucketName = "sri-subscription-terraform-state-$AwsAccountId"
$bucketExists = aws s3 ls "s3://$bucketName" 2>$null
if ($bucketExists) {
    Write-Host "✓ S3 bucket exists" -ForegroundColor Green
} else {
    Write-Host "❌ S3 bucket not found" -ForegroundColor Red
}

# Check DynamoDB table
Write-Host "Checking DynamoDB table..."
$tableName = "sri-subscription-terraform-locks"
$tableExists = aws dynamodb describe-table --table-name $tableName 2>$null
if ($tableExists) {
    Write-Host "✓ DynamoDB table exists" -ForegroundColor Green
} else {
    Write-Host "❌ DynamoDB table not found" -ForegroundColor Red
}

# Check ECR repositories
Write-Host "Checking ECR repositories..."
$repositories = @("identity-service", "api-gateway", "data-service", "frontend")
foreach ($repo in $repositories) {
    $repoName = "sri-subscription-$repo"
    $repoExists = aws ecr describe-repositories --repository-names $repoName 2>$null
    if ($repoExists) {
        Write-Host "✓ ECR repository $repoName exists" -ForegroundColor Green
    } else {
        Write-Host "❌ ECR repository $repoName not found" -ForegroundColor Red
    }
}

# Check VPC
Write-Host "Checking VPC..."
$vpcId = aws ec2 describe-vpcs --filters "Name=tag:Name,Values=sri-subscription-vpc" --query "Vpcs[0].VpcId" --output text
if ($vpcId -and $vpcId -ne "None") {
    Write-Host "✓ VPC exists: $vpcId" -ForegroundColor Green
} else {
    Write-Host "❌ VPC not found" -ForegroundColor Red
}

# Check Cognito
Write-Host "Checking Cognito User Pool..."
$userPoolId = aws cognito-idp list-user-pools --max-items 10 --query "UserPools[?Name=='sri-subscription-user-pool'].Id" --output text
if ($userPoolId) {
    Write-Host "✓ Cognito User Pool exists: $userPoolId" -ForegroundColor Green
} else {
    Write-Host "❌ Cognito User Pool not found" -ForegroundColor Red
}

Write-Host "`n✅ Validation completed!" -ForegroundColor Green
```

## Validation

After completing this phase, verify:

- [ ] IAM policy created and attached (Step 0)
- [ ] Prerequisites checker passes
- [ ] IAM permissions verified
- [ ] Terraform setup completes successfully
- [ ] S3 bucket created for state storage
- [ ] DynamoDB table created for state locking
- [ ] ECR repositories created for all services
- [ ] VPC and networking configured
- [ ] Security groups created
- [ ] Cognito User Pool and Client created
- [ ] CloudWatch log groups created
- [ ] Cost monitoring configured
- [ ] .env files generated correctly
- [ ] GitHub secrets instructions provided
- [ ] Validation script passes

## Next Steps

Proceed to [Phase 5: AWS App Deployment](phase5-aws-deploy.md)

## Cost Optimization Notes

- **No NAT Gateway**: Uses only public subnets to save $32-45/month
- **Free Tier Eligible**: Most resources qualify for AWS Free Tier (first 12 months)
- **Minimal Instance Sizes**: db.t3.micro, 256MB App Runner instances
- **Short Log Retention**: 7 days (vs 14-30 days)
- **ECR Lifecycle Policies**: Automatically delete old images to reduce storage
- **Budget Alerts**: Configured to notify at 80% of monthly budget
- **Pay-Per-Request DynamoDB**: No provisioned capacity costs

## Production Recommendations

When moving to production, consider:

- Enable Multi-AZ for RDS (add ~$12-15/month)
- Increase App Runner CPU/memory (1vCPU/2GB: ~$50-70/month)
- Add private subnets + NAT Gateway for enhanced security (add ~$32-45/month)
- Increase log retention to 30-90 days
- Increase RDS backup retention to 30 days
- Add CloudWatch alarms for monitoring

## Notes

### IAM Policy Management

- **IAM policy is in** `terraform/iam-policy.tf` (separate from setup/)
- This allows admin to create policy ONCE, then regular users can deploy
- Policy is modular - organized into 13 resource groups
- Can be exported to JSON if needed: `terraform output -raw policy_json`

### Infrastructure Setup

- This is a one-time setup per AWS account
- All resources are tagged for cost tracking
- ECR repositories have lifecycle policies to manage costs
- VPC uses only public subnets for cost optimization
- Security groups provide network isolation despite public subnets
- Cost monitoring helps track spending
- Setup script automates the entire process
- Validation script ensures everything was created correctly

### Terraform State

- IAM policy state: `terraform/terraform.tfstate`
- Infrastructure state: `terraform/setup/terraform.tfstate` (initially local, then S3)





