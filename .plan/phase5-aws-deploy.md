# Phase 5: AWS App Deployment

> 📖 **Quick Start?** If you've done this before, see [`phase5-quick-start.md`](phase5-quick-start.md) for a condensed version.  
> 📚 **Detailed Guide:** This document provides comprehensive Terraform configurations and explanations.

## Goal

Deploy the microservices to AWS App Runner and RDS PostgreSQL, with frontend on S3/CloudFront. This phase creates the production-ready infrastructure for running the application in the cloud.

**Cost-Optimized Architecture**: All services run in public subnets to avoid NAT Gateway costs while maintaining security through security groups.

## Prerequisites

- Phase 4 completed (AWS setup infrastructure)
- Docker images built and ready
- Understanding of AWS App Runner and RDS
- Terraform outputs from setup phase

## Estimated Monthly Costs (Dev Environment)

See Phase 4 for detailed cost breakdown. Total: **~$30-40/month** (or **~$0-10/month** with AWS Free Tier)

## Deliverables Checklist

### 1. Terraform Deployment Module

#### Create Deployment Directory Structure

```bash
mkdir -p terraform/deploy/modules/{app-runner,rds,s3-cloudfront,networking}
mkdir -p terraform/deploy/environments
```

#### Main Deployment Configuration

**File**: `terraform/deploy/main.tf`

```hcl
terraform {
  required_version = ">= 1.5.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  backend "s3" {
    bucket         = var.s3_bucket_name
    key            = "deploy/${var.environment}/terraform.tfstate"
    region         = var.aws_region
    encrypt        = true
    dynamodb_table = var.dynamodb_table_name
  }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = var.project_name
      Environment = var.environment
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

# Modules
module "rds" {
  source = "./modules/rds"

  project_name = var.project_name
  environment  = var.environment
  vpc_id       = var.vpc_id
  subnet_ids   = var.public_subnet_ids  # Using public subnets for cost optimization
  security_group_ids = [var.rds_security_group_id]

  instance_class = var.db_instance_class
  multi_az       = var.enable_multi_az
  backup_retention_days = var.backup_retention_days
  publicly_accessible = false  # Still not publicly accessible despite public subnet
}

module "app_runner" {
  source = "./modules/app-runner"

  project_name = var.project_name
  environment  = var.environment
  vpc_id       = var.vpc_id
  subnet_ids   = var.public_subnet_ids  # Using public subnets for cost optimization
  security_group_ids = [var.app_runner_security_group_id]

  ecr_repository_urls = var.ecr_repository_urls
  rds_endpoint = module.rds.endpoint
  rds_port     = module.rds.port
  rds_database = module.rds.database_name
  rds_username = module.rds.username
  rds_password = module.rds.password

  cognito_user_pool_id = var.cognito_user_pool_id
  cognito_user_pool_client_id = var.cognito_user_pool_client_id
}

module "s3_cloudfront" {
  source = "./modules/s3-cloudfront"

  project_name = var.project_name
  environment  = var.environment
  domain_name  = var.domain_name
}
```

#### Variables

**File**: `terraform/deploy/variables.tf`

```hcl
variable "project_name" {
  description = "Project name used for resource naming"
  type        = string
  default     = "sri-subscription"
}

variable "environment" {
  description = "Environment name"
  type        = string

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be dev, staging, or prod."
  }
}

variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "us-east-1"
}

variable "cost_center" {
  description = "Cost center for billing"
  type        = string
  default     = "engineering"
}

# Setup infrastructure references
variable "s3_bucket_name" {
  description = "S3 bucket name for Terraform state"
  type        = string
}

variable "dynamodb_table_name" {
  description = "DynamoDB table name for Terraform state locking"
  type        = string
}

variable "vpc_id" {
  description = "VPC ID from setup"
  type        = string
}

variable "public_subnet_ids" {
  description = "Public subnet IDs from setup (used for both App Runner and RDS)"
  type        = list(string)
}

variable "app_runner_security_group_id" {
  description = "App Runner security group ID from setup"
  type        = string
}

variable "rds_security_group_id" {
  description = "RDS security group ID from setup"
  type        = string
}

variable "ecr_repository_urls" {
  description = "ECR repository URLs from setup"
  type = object({
    identity_service = string
    api_gateway      = string
    data_service     = string
    frontend         = string
  })
}

variable "cognito_user_pool_id" {
  description = "Cognito User Pool ID from setup"
  type        = string
}

variable "cognito_user_pool_client_id" {
  description = "Cognito User Pool Client ID from setup"
  type        = string
}

# Environment-specific variables
variable "db_instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.t3.micro"
}

variable "enable_multi_az" {
  description = "Enable Multi-AZ for RDS"
  type        = bool
  default     = false
}

variable "backup_retention_days" {
  description = "RDS backup retention days"
  type        = number
  default     = 7
}

variable "domain_name" {
  description = "Custom domain name (optional)"
  type        = string
  default     = ""
}
```

### 2. RDS Module

**File**: `terraform/deploy/modules/rds/main.tf`

**Security Note**: RDS is in a public subnet but `publicly_accessible = false` and security groups only allow connections from App Runner services within the VPC.

```hcl
# Random password for RDS
resource "random_password" "db_password" {
  length  = 32
  special = true
}

# RDS Subnet Group (using public subnets for cost optimization)
resource "aws_db_subnet_group" "main" {
  name       = "${var.project_name}-${var.environment}-db-subnet-group"
  subnet_ids = var.subnet_ids

  tags = {
    Name = "${var.project_name}-${var.environment}-db-subnet-group"
  }
}

# RDS Parameter Group
resource "aws_db_parameter_group" "main" {
  family = "postgres15"
  name   = "${var.project_name}-${var.environment}-db-params"

  parameter {
    name  = "log_statement"
    value = "all"
  }

  parameter {
    name  = "log_min_duration_statement"
    value = "1000"
  }

  tags = {
    Name = "${var.project_name}-${var.environment}-db-params"
  }
}

# RDS Instance
resource "aws_db_instance" "main" {
  identifier = "${var.project_name}-${var.environment}-postgres"

  engine         = "postgres"
  engine_version = "15.4"
  instance_class = var.instance_class

  allocated_storage     = 20
  max_allocated_storage = 100
  storage_type          = "gp2"
  storage_encrypted     = true

  db_name  = "${var.project_name}_${var.environment}"
  username = "postgres"
  password = random_password.db_password.result

  vpc_security_group_ids = var.security_group_ids
  db_subnet_group_name   = aws_db_subnet_group.main.name
  parameter_group_name   = aws_db_parameter_group.main.name

  # Public subnet but not publicly accessible for security
  publicly_accessible = var.publicly_accessible

  multi_az               = var.multi_az
  backup_retention_period = var.backup_retention_days
  backup_window          = "03:00-04:00"
  maintenance_window     = "mon:04:00-mon:05:00"

  skip_final_snapshot       = var.environment != "prod"
  final_snapshot_identifier = var.environment == "prod" ? "${var.project_name}-${var.environment}-final-snapshot" : null

  enabled_cloudwatch_logs_exports = ["postgresql", "upgrade"]

  tags = {
    Name = "${var.project_name}-${var.environment}-postgres"
  }
}

# Store password in Secrets Manager
resource "aws_secretsmanager_secret" "db_password" {
  name                    = "${var.project_name}/${var.environment}/db-password"
  recovery_window_in_days = var.environment == "prod" ? 30 : 0

  tags = {
    Name = "${var.project_name}-${var.environment}-db-password"
  }
}

resource "aws_secretsmanager_secret_version" "db_password" {
  secret_id = aws_secretsmanager_secret.db_password.id
  secret_string = jsonencode({
    username = aws_db_instance.main.username
    password = random_password.db_password.result
    host     = aws_db_instance.main.endpoint
    port     = aws_db_instance.main.port
    database = aws_db_instance.main.db_name
  })
}
```

**File**: `terraform/deploy/modules/rds/variables.tf`

```hcl
variable "project_name" {
  description = "Project name"
  type        = string
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "vpc_id" {
  description = "VPC ID"
  type        = string
}

variable "subnet_ids" {
  description = "Subnet IDs for RDS"
  type        = list(string)
}

variable "security_group_ids" {
  description = "Security group IDs for RDS"
  type        = list(string)
}

variable "instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.t3.micro"
}

variable "multi_az" {
  description = "Enable Multi-AZ"
  type        = bool
  default     = false
}

variable "backup_retention_days" {
  description = "Backup retention days"
  type        = number
  default     = 7
}

variable "publicly_accessible" {
  description = "Whether RDS is publicly accessible (should be false even in public subnet)"
  type        = bool
  default     = false
}

# Note: Tags are applied via provider default_tags
```

**File**: `terraform/deploy/modules/rds/outputs.tf`

```hcl
output "endpoint" {
  description = "RDS endpoint"
  value       = aws_db_instance.main.endpoint
}

output "port" {
  description = "RDS port"
  value       = aws_db_instance.main.port
}

output "database_name" {
  description = "Database name"
  value       = aws_db_instance.main.db_name
}

output "username" {
  description = "Database username"
  value       = aws_db_instance.main.username
}

output "password" {
  description = "Database password"
  value       = random_password.db_password.result
  sensitive   = true
}

output "secret_arn" {
  description = "Secrets Manager secret ARN"
  value       = aws_secretsmanager_secret.db_password.arn
}
```

### 3. App Runner Module

**File**: `terraform/deploy/modules/app-runner/main.tf`

```hcl
# IAM role for App Runner services
resource "aws_iam_role" "app_runner" {
  name = "${var.project_name}-${var.environment}-app-runner-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "build.apprunner.amazonaws.com"
        }
      }
    ]
  })

  tags = {
    Name = "${var.project_name}-${var.environment}-app-runner-role"
  }
}

# IAM policy for App Runner
resource "aws_iam_policy" "app_runner" {
  name = "${var.project_name}-${var.environment}-app-runner-policy"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "secretsmanager:GetSecretValue"
        ]
        Resource = [
          "arn:aws:secretsmanager:${data.aws_region.current.name}:${data.aws_caller_identity.current.account_id}:secret:${var.project_name}/${var.environment}/*"
        ]
      }
    ]
  })

  tags = {
    Name = "${var.project_name}-${var.environment}-app-runner-policy"
  }
}

# Attach policy to role
resource "aws_iam_role_policy_attachment" "app_runner" {
  role       = aws_iam_role.app_runner.name
  policy_arn = aws_iam_policy.app_runner.arn
}

# Data sources
data "aws_region" "current" {}
data "aws_caller_identity" "current" {}

# Identity Service App Runner
resource "aws_apprunner_service" "identity_service" {
  service_name = "${var.project_name}-${var.environment}-identity-service"

  source_configuration {
    authentication_configuration {
      access_role_arn = aws_iam_role.app_runner_ecr.arn
    }

    image_repository {
      image_identifier      = "${var.ecr_repository_urls.identity_service}:latest"
      image_repository_type = "ECR"

      image_configuration {
        port = "8080"

        runtime_environment_variables = {
          ASPNETCORE_ENVIRONMENT               = title(var.environment)
          ConnectionStrings__DefaultConnection = "Host=${var.rds_endpoint};Port=${var.rds_port};Database=${var.rds_database};Username=${var.rds_username};Password=${var.rds_password}"
          Cognito__UserPoolId                  = var.cognito_user_pool_id
          Cognito__AppClientId                 = var.cognito_user_pool_client_id
          Cognito__Domain                      = var.cognito_domain
          Cognito__Region                      = data.aws_region.current.name
        }
      }
    }

    auto_deployments_enabled = false
  }

  instance_configuration {
    cpu               = var.cpu
    memory            = var.memory
    instance_role_arn = aws_iam_role.app_runner_instance.arn
  }

  network_configuration {
    egress_configuration {
      egress_type       = "VPC"
      vpc_connector_arn = aws_apprunner_vpc_connector.main.arn
    }
  }

  health_check_configuration {
    protocol            = "HTTP"
    path                = "/health"
    interval            = 10
    timeout             = 5
    healthy_threshold   = 1
    unhealthy_threshold = 5
  }

  tags = {
    Name = "${var.project_name}-${var.environment}-identity-service"
  }
}

# Data Service App Runner
resource "aws_apprunner_service" "data_service" {
  service_name = "${var.project_name}-${var.environment}-data-service"

  source_configuration {
    authentication_configuration {
      access_role_arn = aws_iam_role.app_runner_ecr.arn
    }

    image_repository {
      image_identifier      = "${var.ecr_repository_urls.data_service}:latest"
      image_repository_type = "ECR"

      image_configuration {
        port = "8080"

        runtime_environment_variables = {
          ASPNETCORE_ENVIRONMENT               = title(var.environment)
          ConnectionStrings__DefaultConnection = "Host=${var.rds_endpoint};Port=${var.rds_port};Database=${var.rds_database};Username=${var.rds_username};Password=${var.rds_password}"
          Cognito__UserPoolId                  = var.cognito_user_pool_id
          Cognito__AppClientId                 = var.cognito_user_pool_client_id
          Cognito__Domain                      = var.cognito_domain
          Cognito__Region                      = data.aws_region.current.name
        }
      }
    }

    auto_deployments_enabled = false
  }

  instance_configuration {
    cpu               = var.cpu
    memory            = var.memory
    instance_role_arn = aws_iam_role.app_runner_instance.arn
  }

  network_configuration {
    egress_configuration {
      egress_type       = "VPC"
      vpc_connector_arn = aws_apprunner_vpc_connector.main.arn
    }
  }

  health_check_configuration {
    protocol            = "HTTP"
    path                = "/health"
    interval            = 10
    timeout             = 5
    healthy_threshold   = 1
    unhealthy_threshold = 5
  }

  tags = {
    Name = "${var.project_name}-${var.environment}-data-service"
  }
}

# API Gateway Service
resource "aws_apprunner_service" "api_gateway" {
  service_name = "${var.project_name}-${var.environment}-api-gateway"

  source_configuration {
    authentication_configuration {
      access_role_arn = aws_iam_role.app_runner_ecr.arn
    }

    image_repository {
      image_identifier      = "${var.ecr_repository_urls.api_gateway}:latest"
      image_repository_type = "ECR"

      image_configuration {
        port = "8080"

        runtime_environment_variables = {
          ASPNETCORE_ENVIRONMENT    = title(var.environment)
          IdentityServiceUrl        = "https://${aws_apprunner_service.identity_service.service_url}"
          DataServiceUrl            = "https://${aws_apprunner_service.data_service.service_url}"
          Cognito__UserPoolId       = var.cognito_user_pool_id
          Cognito__AppClientId      = var.cognito_user_pool_client_id
          Cognito__Domain           = var.cognito_domain
          Cognito__Region           = data.aws_region.current.name
        }
      }
    }

    auto_deployments_enabled = false
  }

  instance_configuration {
    cpu               = var.cpu
    memory            = var.memory
    instance_role_arn = aws_iam_role.app_runner_instance.arn
  }

  network_configuration {
    egress_configuration {
      egress_type       = "VPC"
      vpc_connector_arn = aws_apprunner_vpc_connector.main.arn
    }
  }

  health_check_configuration {
    protocol            = "HTTP"
    path                = "/health"
    interval            = 10
    timeout             = 5
    healthy_threshold   = 1
    unhealthy_threshold = 5
  }

  tags = {
    Name = "${var.project_name}-${var.environment}-api-gateway"
  }

  depends_on = [
    aws_apprunner_service.identity_service,
    aws_apprunner_service.data_service
  ]
}

# VPC Connector for App Runner
resource "aws_apprunner_vpc_connector" "main" {
  vpc_connector_name = "${var.project_name}-${var.environment}-vpc-connector"
  subnets           = var.subnet_ids
  security_groups   = var.security_group_ids

  tags = {
    Name = "${var.project_name}-${var.environment}-vpc-connector"
  }
}
```

### 4. S3/CloudFront Module

**File**: `terraform/deploy/modules/s3-cloudfront/main.tf`

```hcl
# S3 bucket for frontend
resource "aws_s3_bucket" "frontend" {
  bucket = "${var.project_name}-${var.environment}-frontend"

  tags = {
    Name = "${var.project_name}-${var.environment}-frontend"
  }
}

# S3 bucket versioning
resource "aws_s3_bucket_versioning" "frontend" {
  bucket = aws_s3_bucket.frontend.id
  versioning_configuration {
    status = "Enabled"
  }
}

# S3 bucket encryption
resource "aws_s3_bucket_server_side_encryption_configuration" "frontend" {
  bucket = aws_s3_bucket.frontend.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

# S3 bucket public access block
resource "aws_s3_bucket_public_access_block" "frontend" {
  bucket = aws_s3_bucket.frontend.id

  block_public_acls       = false
  block_public_policy     = false
  ignore_public_acls      = false
  restrict_public_buckets = false
}

# S3 bucket policy for CloudFront
resource "aws_s3_bucket_policy" "frontend" {
  bucket = aws_s3_bucket.frontend.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid       = "AllowCloudFrontServicePrincipal"
        Effect    = "Allow"
        Principal = {
          Service = "cloudfront.amazonaws.com"
        }
        Action   = "s3:GetObject"
        Resource = "${aws_s3_bucket.frontend.arn}/*"
        Condition = {
          StringEquals = {
            "AWS:SourceArn" = aws_cloudfront_distribution.frontend.arn
          }
        }
      }
    ]
  })
}

# CloudFront Origin Access Control
resource "aws_cloudfront_origin_access_control" "frontend" {
  name                              = "${var.project_name}-${var.environment}-oac"
  description                       = "OAC for ${var.project_name} frontend"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

# CloudFront distribution
resource "aws_cloudfront_distribution" "frontend" {
  origin {
    domain_name              = aws_s3_bucket.frontend.bucket_regional_domain_name
    origin_access_control_id = aws_cloudfront_origin_access_control.frontend.id
    origin_id                = "S3-${aws_s3_bucket.frontend.bucket}"
  }

  enabled             = true
  is_ipv6_enabled     = true
  default_root_object = "index.html"

  default_cache_behavior {
    allowed_methods        = ["DELETE", "GET", "HEAD", "OPTIONS", "PATCH", "POST", "PUT"]
    cached_methods         = ["GET", "HEAD"]
    target_origin_id       = "S3-${aws_s3_bucket.frontend.bucket}"
    compress               = true
    viewer_protocol_policy = "redirect-to-https"

    forwarded_values {
      query_string = false
      cookies {
        forward = "none"
      }
    }

    min_ttl     = 0
    default_ttl = 3600
    max_ttl     = 86400
  }

  # Cache behavior for static assets
  ordered_cache_behavior {
    path_pattern     = "/assets/*"
    allowed_methods  = ["GET", "HEAD", "OPTIONS"]
    cached_methods   = ["GET", "HEAD"]
    target_origin_id = "S3-${aws_s3_bucket.frontend.bucket}"
    compress         = true

    forwarded_values {
      query_string = false
      cookies {
        forward = "none"
      }
    }

    min_ttl     = 0
    default_ttl = 31536000
    max_ttl     = 31536000
  }

  # Custom error pages for SPA
  custom_error_response {
    error_code         = 404
    response_code      = 200
    response_page_path = "/index.html"
  }

  custom_error_response {
    error_code         = 403
    response_code      = 200
    response_page_path = "/index.html"
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    cloudfront_default_certificate = true
  }

  tags = {
    Name = "${var.project_name}-${var.environment}-cloudfront"
  }
}
```

### 5. Environment Configuration Files

**File**: `terraform/deploy/environments/dev.tfvars`

```hcl
environment = "dev"
db_instance_class = "db.t3.micro"  # Free Tier eligible
enable_multi_az = false            # Single AZ to save costs
backup_retention_days = 7          # Minimum for cost savings
```

**File**: `terraform/deploy/environments/staging.tfvars`

```hcl
environment = "staging"
db_instance_class = "db.t3.small"  # Slightly larger for staging tests
enable_multi_az = false            # Single AZ to save costs
backup_retention_days = 14
```

**File**: `terraform/deploy/environments/prod.tfvars`

```hcl
environment = "prod"
db_instance_class = "db.t3.medium"  # Production workload
enable_multi_az = true              # High availability (adds ~$15/month)
backup_retention_days = 30          # Extended backups for production
```

### 6. Deployment Scripts

**File**: `scripts/deploy-dev.ps1`

```powershell
# Deploy to dev environment
Write-Host "🚀 Deploying to dev environment..." -ForegroundColor Green

# Build and push Docker images
Write-Host "`n📦 Building and pushing Docker images..." -ForegroundColor Yellow
& "$PSScriptRoot/build-and-push.ps1"

# Deploy infrastructure
Write-Host "`n🏗️ Deploying infrastructure..." -ForegroundColor Yellow
Set-Location "terraform/deploy"
terraform init
terraform plan -var-file="environments/dev.tfvars"
terraform apply -var-file="environments/dev.tfvars" -auto-approve

# Deploy frontend
Write-Host "`n🌐 Deploying frontend..." -ForegroundColor Yellow
& "$PSScriptRoot/deploy-frontend.ps1" -Environment "dev"

Write-Host "`n✅ Dev deployment completed!" -ForegroundColor Green
```

**File**: `scripts/build-and-push.ps1`

```powershell
# Build and push Docker images to ECR
param(
    [string]$Environment = "dev"
)

Write-Host "🔨 Building Docker images..." -ForegroundColor Yellow

# Get ECR repository URLs from GitHub secrets or environment variables
$ecrIdentity = $env:ECR_IDENTITY_SERVICE_URL
$ecrGateway = $env:ECR_API_GATEWAY_URL
$ecrData = $env:ECR_DATA_SERVICE_URL
$ecrFrontend = $env:ECR_FRONTEND_URL

# Login to ECR
Write-Host "🔐 Logging in to ECR..." -ForegroundColor Yellow
aws ecr get-login-password --region $env:AWS_REGION | docker login --username AWS --password-stdin $ecrIdentity

# Build and push Identity Service
Write-Host "Building Identity Service..." -ForegroundColor Yellow
docker build -t identity-service ./backend/IdentityService
docker tag identity-service:latest "$ecrIdentity:latest"
docker push "$ecrIdentity:latest"

# Build and push API Gateway
Write-Host "Building API Gateway..." -ForegroundColor Yellow
docker build -t api-gateway ./backend/ApiGateway
docker tag api-gateway:latest "$ecrGateway:latest"
docker push "$ecrGateway:latest"

# Build and push Data Service
Write-Host "Building Data Service..." -ForegroundColor Yellow
docker build -t data-service ./backend/DataService
docker tag data-service:latest "$ecrData:latest"
docker push "$ecrData:latest"

# Build and push Frontend
Write-Host "Building Frontend..." -ForegroundColor Yellow
docker build -t frontend ./frontend
docker tag frontend:latest "$ecrFrontend:latest"
docker push "$ecrFrontend:latest"

Write-Host "✅ All images built and pushed!" -ForegroundColor Green
```

**File**: `scripts/deploy-frontend.ps1`

```powershell
# Deploy frontend to S3/CloudFront
param(
    [string]$Environment = "dev"
)

Write-Host "🌐 Deploying frontend to S3/CloudFront..." -ForegroundColor Yellow

# Build frontend
Write-Host "Building frontend..." -ForegroundColor Yellow
Set-Location "../frontend"
npm run build

# Get S3 bucket name from Terraform output
Set-Location "../terraform/deploy"
$bucketName = terraform output -raw s3_bucket_name

# Sync to S3
Write-Host "Syncing to S3..." -ForegroundColor Yellow
aws s3 sync ../frontend/dist/ "s3://$bucketName" --delete

# Invalidate CloudFront cache
Write-Host "Invalidating CloudFront cache..." -ForegroundColor Yellow
$distributionId = terraform output -raw cloudfront_distribution_id
aws cloudfront create-invalidation --distribution-id $distributionId --paths "/*"

Write-Host "✅ Frontend deployed!" -ForegroundColor Green
```

### 7. Health Check Scripts

**File**: `scripts/health-check.ps1`

```powershell
# Health check for deployed services
param(
    [string]$Environment = "dev"
)

Write-Host "🔍 Checking service health..." -ForegroundColor Yellow

# Get service URLs from Terraform output
Set-Location "terraform/deploy"
$identityUrl = terraform output -raw identity_service_url
$gatewayUrl = terraform output -raw api_gateway_url
$dataUrl = terraform output -raw data_service_url
$frontendUrl = terraform output -raw frontend_url

# Check Identity Service
Write-Host "Checking Identity Service..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$identityUrl/health" -Method Get
    Write-Host "✓ Identity Service: $($response.status)" -ForegroundColor Green
} catch {
    Write-Host "❌ Identity Service: $($_.Exception.Message)" -ForegroundColor Red
}

# Check API Gateway
Write-Host "Checking API Gateway..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$gatewayUrl/health" -Method Get
    Write-Host "✓ API Gateway: $($response.status)" -ForegroundColor Green
} catch {
    Write-Host "❌ API Gateway: $($_.Exception.Message)" -ForegroundColor Red
}

# Check Data Service
Write-Host "Checking Data Service..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$dataUrl/health" -Method Get
    Write-Host "✓ Data Service: $($response.status)" -ForegroundColor Green
} catch {
    Write-Host "❌ Data Service: $($_.Exception.Message)" -ForegroundColor Red
}

# Check Frontend
Write-Host "Checking Frontend..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri $frontendUrl -Method Get
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ Frontend: OK" -ForegroundColor Green
    } else {
        Write-Host "❌ Frontend: HTTP $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Frontend: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n✅ Health check completed!" -ForegroundColor Green
```

## Deployment Outputs

After successful deployment, Terraform will output the following values:

### Required Outputs (Used by GitHub Actions)

```hcl
# terraform/deploy/outputs.tf

# RDS Outputs
output "rds_endpoint" {
  description = "RDS instance endpoint"
  value       = module.rds.endpoint
  sensitive   = true
}

# App Runner Outputs
output "identity_service_url" {
  description = "Identity Service App Runner URL"
  value       = module.app_runner.identity_service_url
}

output "api_gateway_url" {
  description = "API Gateway App Runner URL"
  value       = module.app_runner.api_gateway_url
}

output "data_service_url" {
  description = "Data Service App Runner URL"
  value       = module.app_runner.data_service_url
}

# Frontend Outputs (with workflow-compatible aliases)
output "cloudfront_distribution_id" {
  description = "CloudFront distribution ID"
  value       = module.s3_cloudfront.cloudfront_distribution_id
}

output "cloudfront_domain_name" {
  description = "CloudFront domain name (for GitHub Actions)"
  value       = module.s3_cloudfront.cloudfront_distribution_domain
}

output "frontend_s3_bucket_name" {
  description = "S3 bucket name (for GitHub Actions)"
  value       = module.s3_cloudfront.s3_bucket_name
}

output "frontend_url" {
  description = "Frontend URL (CloudFront or custom domain)"
  value       = var.domain_name != "" ? "https://${var.domain_name}" : "https://${module.s3_cloudfront.cloudfront_distribution_domain}"
}
```

### Viewing Outputs

```powershell
# View all outputs
cd terraform/deploy
terraform output

# View specific output
terraform output identity_service_url
terraform output -raw rds_endpoint

# Export for scripts
$IDENTITY_URL = terraform output -raw identity_service_url
$API_GATEWAY_URL = terraform output -raw api_gateway_url
$DATA_SERVICE_URL = terraform output -raw data_service_url
$FRONTEND_URL = terraform output -raw frontend_url
```

### Example Output

```
Outputs:

api_gateway_url = "https://abc123.us-east-1.awsapprunner.com"
cloudfront_distribution_id = "E1234567890ABC"
cloudfront_domain_name = "d111111abcdef8.cloudfront.net"
data_service_url = "https://def456.us-east-1.awsapprunner.com"
frontend_s3_bucket_name = "sri-template-dev-frontend"
frontend_url = "https://d111111abcdef8.cloudfront.net"
identity_service_url = "https://ghi789.us-east-1.awsapprunner.com"
rds_endpoint = <sensitive>
rds_database_name = "sri_template_db"
```

## Validation

After completing this phase, verify:

- [ ] Terraform deployment completes successfully
- [ ] RDS instance is running and accessible
- [ ] App Runner services are running
- [ ] Frontend is accessible via CloudFront
- [ ] All services respond to health checks
- [ ] Database migrations run successfully
- [ ] Services can communicate with each other
- [ ] S3 bucket contains frontend files
- [ ] CloudFront distribution is working
- [ ] Cost monitoring is active

## Next Steps

Proceed to [Phase 6: Authentication (Cognito)](phase6-auth.md)

## Cost Optimization Summary

**What We're Saving:**

- **NAT Gateway**: $0 (saves $32-45/month) - Using public subnets instead
- **Small instances**: db.t3.micro, 256MB App Runner
- **Single AZ**: dev/staging use single AZ (saves ~$15/month)
- **Short log retention**: 7 days for dev
- **Minimal backups**: 7 days for dev

**Current Cost Estimate (Dev):**

- RDS: ~$12-15/month (Free Tier eligible)
- App Runner (3 services): ~$15-25/month
- S3 + CloudFront: <$2/month (Free Tier eligible)
- **Total: ~$30-40/month** (or **~$0-10/month with Free Tier**)

**Security Notes:**

- Public subnets do NOT mean public access
- RDS has `publicly_accessible = false`
- Security groups restrict all network access
- Only App Runner services can reach RDS
- Same security level as private subnets, just cheaper

## Notes

- App Runner services are configured with VPC connectivity
- RDS is in public subnets but NOT publicly accessible (security groups enforce access control)
- Frontend is served via S3/CloudFront for performance
- All services use environment-specific configurations
- Health checks ensure services are running correctly
- Cost optimization is built into the configuration
- Services can scale automatically based on demand
- Security is maintained through security groups and network ACLs





