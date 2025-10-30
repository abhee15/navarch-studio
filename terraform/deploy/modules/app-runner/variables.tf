variable "project_name" {
  description = "Project name used for resource naming"
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
  description = "Subnet IDs for VPC connector"
  type        = list(string)
}

variable "security_group_ids" {
  description = "Security group IDs for App Runner"
  type        = list(string)
}

variable "ecr_repository_urls" {
  description = "ECR repository URLs"
  type = object({
    identity_service = string
    api_gateway      = string
    data_service     = string
    frontend         = string
  })
}

variable "rds_endpoint" {
  description = "RDS endpoint"
  type        = string
}

variable "rds_port" {
  description = "RDS port"
  type        = number
}

variable "rds_database" {
  description = "RDS database name"
  type        = string
}

variable "rds_username" {
  description = "RDS username"
  type        = string
  sensitive   = true
}

variable "rds_password" {
  description = "RDS password"
  type        = string
  sensitive   = true
}

variable "cognito_user_pool_id" {
  description = "Cognito User Pool ID"
  type        = string
}

variable "cognito_user_pool_client_id" {
  description = "Cognito User Pool Client ID"
  type        = string
}

variable "cognito_domain" {
  description = "Cognito Domain"
  type        = string
}

variable "cpu" {
  description = "CPU units for App Runner"
  type        = string
  default     = "256" # 0.25 vCPU
}

variable "memory" {
  description = "Memory for App Runner (MB)"
  type        = string
  default     = "512" # 512 MB
}

variable "cloudfront_distribution_domain" {
  description = "CloudFront distribution domain for CORS (e.g., https://d123abc.cloudfront.net)"
  type        = string
  default     = ""
}

variable "benchmark_raw_bucket" {
  description = "Benchmark raw S3 bucket name"
  type        = string
  default     = ""
}

variable "benchmark_curated_bucket" {
  description = "Benchmark curated S3 bucket name"
  type        = string
  default     = ""
}
