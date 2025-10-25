variable "project_name" {
  description = "Project name used for resource naming"
  type        = string
  default     = "navarch-studio"

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





