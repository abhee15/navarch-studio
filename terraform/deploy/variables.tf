# Project Configuration
variable "project_name" {
  description = "Project name used for resource naming"
  type        = string
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

# Backend Configuration (passed but not used directly - used by backend config)
variable "s3_bucket_name" {
  description = "S3 bucket name for Terraform state (used in backend config)"
  type        = string
}

variable "dynamodb_table_name" {
  description = "DynamoDB table name for Terraform state locking (used in backend config)"
  type        = string
}

# Setup Infrastructure References (from Phase 4)
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

variable "cognito_domain" {
  description = "Cognito Domain from setup"
  type        = string
}

# RDS Configuration
variable "db_instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.t3.micro" # Free Tier eligible
}

variable "db_allocated_storage" {
  description = "Allocated storage for RDS (GB)"
  type        = number
  default     = 20 # Free Tier: 20 GB
}

variable "enable_multi_az" {
  description = "Enable Multi-AZ for RDS (increases cost)"
  type        = bool
  default     = false # Set to true for production
}

variable "backup_retention_days" {
  description = "Number of days to retain backups"
  type        = number
  default     = 7
}

# App Runner Configuration
variable "app_runner_cpu" {
  description = "CPU units for App Runner services (256 = 0.25 vCPU, 512 = 0.5 vCPU, 1024 = 1 vCPU)"
  type        = string
  default     = "256" # 0.25 vCPU - minimal for dev
}

variable "app_runner_memory" {
  description = "Memory for App Runner services (MB)"
  type        = string
  default     = "512" # 512 MB - minimal for dev
}

# Frontend Configuration
variable "domain_name" {
  description = "Custom domain name for CloudFront (optional)"
  type        = string
  default     = ""
}






