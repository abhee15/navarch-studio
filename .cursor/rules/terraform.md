# Terraform Rules

## General Guidelines

- Use Terraform 1.5+ features
- Follow HashiCorp's style conventions
- Use modules for reusable infrastructure
- Keep state files in S3 with DynamoDB locking
- Use workspaces or separate state files per environment
- Always use `terraform fmt` before committing

## Project Structure

```
terraform/
├── setup/                    # One-time infrastructure
│   ├── main.tf
│   ├── variables.tf
│   ├── outputs.tf
│   ├── backend.tf           # S3 backend config
│   └── versions.tf          # Provider versions
│
└── deploy/                   # Application infrastructure
    ├── main.tf
    ├── variables.tf
    ├── outputs.tf
    ├── backend.tf
    ├── versions.tf
    ├── modules/              # Reusable modules
    │   ├── app-runner/
    │   ├── rds/
    │   ├── s3-cloudfront/
    │   └── cognito/
    └── environments/         # Environment configs
        ├── dev.tfvars
        ├── staging.tfvars
        └── prod.tfvars
```

## Naming Conventions

```hcl
# Resources: service-environment-resource
resource "aws_s3_bucket" "frontend_dev" {
  bucket = "${var.project_name}-frontend-${var.environment}"
}

# Variables: snake_case
variable "instance_type" {
  description = "EC2 instance type"
  type        = string
  default     = "t3.micro"
}

# Locals: snake_case
locals {
  common_tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}
```

## versions.tf

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
      Environment = var.environment
      ManagedBy   = "Terraform"
    }
  }
}
```

## backend.tf (for deploy, not setup)

```hcl
terraform {
  backend "s3" {
    bucket         = "sri-subscription-terraform-state"
    key            = "deploy/${var.environment}/terraform.tfstate"
    region         = "us-east-1"
    encrypt        = true
    dynamodb_table = "sri-subscription-terraform-locks"
  }
}
```

## variables.tf

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

variable "aws_account_id" {
  description = "AWS account ID"
  type        = string
  sensitive   = true
}

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
```

## outputs.tf

```hcl
output "ecr_repository_urls" {
  description = "ECR repository URLs"
  value = {
    identity_service = aws_ecr_repository.identity_service.repository_url
    api_gateway      = aws_ecr_repository.api_gateway.repository_url
    data_service     = aws_ecr_repository.data_service.repository_url
  }
}

output "rds_endpoint" {
  description = "RDS endpoint"
  value       = aws_db_instance.postgres.endpoint
  sensitive   = true
}

output "cloudfront_domain" {
  description = "CloudFront distribution domain"
  value       = aws_cloudfront_distribution.frontend.domain_name
}
```

## Module Structure

```hcl
# modules/rds/main.tf
resource "aws_db_instance" "this" {
  identifier        = "${var.project_name}-${var.environment}-postgres"
  engine            = "postgres"
  engine_version    = "15.4"
  instance_class    = var.instance_class
  allocated_storage = var.allocated_storage
  storage_encrypted = true

  db_name  = var.database_name
  username = var.master_username
  password = var.master_password

  vpc_security_group_ids = var.security_group_ids
  db_subnet_group_name   = var.db_subnet_group_name

  multi_az               = var.multi_az
  backup_retention_period = var.backup_retention_days
  backup_window          = "03:00-04:00"
  maintenance_window     = "mon:04:00-mon:05:00"

  skip_final_snapshot       = var.environment != "prod"
  final_snapshot_identifier = var.environment == "prod" ? "${var.project_name}-${var.environment}-final-snapshot" : null

  enabled_cloudwatch_logs_exports = ["postgresql", "upgrade"]

  tags = merge(
    var.tags,
    {
      Name = "${var.project_name}-${var.environment}-postgres"
    }
  )
}

# modules/rds/variables.tf
variable "project_name" {
  description = "Project name"
  type        = string
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "instance_class" {
  description = "RDS instance class"
  type        = string
}

variable "allocated_storage" {
  description = "Allocated storage in GB"
  type        = number
  default     = 20
}

variable "multi_az" {
  description = "Enable Multi-AZ"
  type        = bool
  default     = false
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}

# modules/rds/outputs.tf
output "endpoint" {
  description = "RDS endpoint"
  value       = aws_db_instance.this.endpoint
}

output "port" {
  description = "RDS port"
  value       = aws_db_instance.this.port
}

output "database_name" {
  description = "Database name"
  value       = aws_db_instance.this.db_name
}
```

## Using Modules

```hcl
# deploy/main.tf
module "rds" {
  source = "./modules/rds"

  project_name  = var.project_name
  environment   = var.environment
  instance_class = var.db_instance_class
  multi_az      = var.enable_multi_az

  security_group_ids   = [aws_security_group.rds.id]
  db_subnet_group_name = aws_db_subnet_group.main.name

  database_name   = var.database_name
  master_username = var.db_username
  master_password = random_password.db_password.result

  tags = local.common_tags
}
```

## Secrets Management

```hcl
# Generate password
resource "random_password" "db_password" {
  length  = 32
  special = true
}

# Store in Secrets Manager
resource "aws_secretsmanager_secret" "db_password" {
  name                    = "${var.project_name}/${var.environment}/db-password"
  recovery_window_in_days = var.environment == "prod" ? 30 : 0
}

resource "aws_secretsmanager_secret_version" "db_password" {
  secret_id = aws_secretsmanager_secret.db_password.id
  secret_string = jsonencode({
    username = var.db_username
    password = random_password.db_password.result
    host     = module.rds.endpoint
    port     = module.rds.port
    database = module.rds.database_name
  })
}
```

## Data Sources

```hcl
# Get current AWS account
data "aws_caller_identity" "current" {}

# Get available AZs
data "aws_availability_zones" "available" {
  state = "available"
}

# Get existing VPC
data "aws_vpc" "main" {
  tags = {
    Name = "${var.project_name}-vpc"
  }
}
```

## Locals

```hcl
locals {
  common_tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = "Terraform"
    CostCenter  = var.cost_center
  }

  azs = slice(data.aws_availability_zones.available.names, 0, 2)

  app_runner_services = {
    identity = {
      name = "identity-service"
      port = 5001
    }
    gateway = {
      name = "api-gateway"
      port = 5002
    }
    data = {
      name = "data-service"
      port = 5003
    }
  }
}
```

## Environment-specific tfvars

```hcl
# environments/dev.tfvars
environment           = "dev"
aws_region           = "us-east-1"
db_instance_class    = "db.t3.micro"
enable_multi_az      = false
app_runner_cpu       = "256"
app_runner_memory    = "512"
backup_retention_days = 7

# environments/prod.tfvars
environment           = "prod"
aws_region           = "us-east-1"
db_instance_class    = "db.t3.small"
enable_multi_az      = true
app_runner_cpu       = "1024"
app_runner_memory    = "2048"
backup_retention_days = 30
```

## Best Practices

- ✅ Use `terraform fmt` before committing
- ✅ Use `terraform validate` to check syntax
- ✅ Use remote state (S3 + DynamoDB)
- ✅ Use modules for reusability
- ✅ Use variables with validation
- ✅ Use outputs for important values
- ✅ Tag all resources consistently
- ✅ Use data sources to reference existing resources
- ✅ Use `depends_on` sparingly (implicit better)
- ✅ Use lifecycle rules when needed
- ✅ Store secrets in AWS Secrets Manager
- ✅ Use separate state files per environment

## Avoid

- ❌ Hardcoding values
- ❌ Committing `.tfvars` files with secrets
- ❌ Using default provider credentials in code
- ❌ Not using modules for repeated resources
- ❌ Ignoring `terraform plan` output
- ❌ Not testing destroy before prod
- ❌ Mixing setup and deploy infrastructure
- ❌ Large monolithic configurations

## Commands

```bash
# Format code
terraform fmt -recursive

# Initialize
terraform init

# Validate
terraform validate

# Plan with tfvars
terraform plan -var-file=environments/dev.tfvars

# Apply with tfvars
terraform apply -var-file=environments/dev.tfvars

# Destroy with tfvars
terraform destroy -var-file=environments/dev.tfvars

# Import existing resource
terraform import aws_s3_bucket.example my-bucket-name
```






