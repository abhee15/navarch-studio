variable "project_name" {
  description = "Project name used for resource naming"
  type        = string
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "domain_name" {
  description = "Custom domain name for CloudFront (optional)"
  type        = string
  default     = ""
}

variable "acm_certificate_arn" {
  description = "ACM certificate ARN for custom domain (required if domain_name is set)"
  type        = string
  default     = ""
}

variable "price_class" {
  description = "CloudFront price class"
  type        = string
  default     = "PriceClass_100" # US, Canada, Europe (cheapest)
}

variable "api_gateway_url" {
  description = "API Gateway URL for frontend configuration"
  type        = string
  default     = "" # Optional - frontend can be deployed without backend
}

variable "auth_mode" {
  description = "Authentication mode (cognito or local)"
  type        = string
  default     = "cognito"
}

variable "cognito_user_pool_id" {
  description = "Cognito User Pool ID"
  type        = string
  default     = ""
}

variable "cognito_client_id" {
  description = "Cognito User Pool Client ID"
  type        = string
  default     = ""
}

variable "aws_region" {
  description = "AWS Region"
  type        = string
}






