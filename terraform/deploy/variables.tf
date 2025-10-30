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

# NOTE: All setup infrastructure values (VPC, subnets, security groups, ECR, Cognito)
# are now pulled from terraform_remote_state in main.tf - no need to pass as variables!
# This ensures single source of truth and automatic updates when setup changes.

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

# CORS Configuration
variable "cloudfront_domain_override" {
  description = "CloudFront domain for CORS configuration (used in second apply to update API Gateway CORS)"
  type        = string
  default     = ""
}