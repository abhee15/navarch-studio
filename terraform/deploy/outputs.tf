# RDS Outputs
output "rds_endpoint" {
  description = "RDS instance endpoint"
  value       = module.rds.endpoint
  # Note: Endpoint is not sensitive (it's just a hostname)
  # Credentials (username/password) remain sensitive
}

output "rds_database_name" {
  description = "RDS database name"
  value       = module.rds.database_name
}

output "rds_username" {
  description = "RDS master username"
  value       = module.rds.username
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

# Frontend Outputs
output "cloudfront_distribution_id" {
  description = "CloudFront distribution ID"
  value       = module.s3_cloudfront.cloudfront_distribution_id
}

output "cloudfront_distribution_domain" {
  description = "CloudFront distribution domain name"
  value       = module.s3_cloudfront.cloudfront_distribution_domain
}

output "cloudfront_domain_name" {
  description = "CloudFront distribution domain name (alias for workflow compatibility)"
  value       = module.s3_cloudfront.cloudfront_distribution_domain
}

output "frontend_s3_bucket" {
  description = "S3 bucket name for frontend"
  value       = module.s3_cloudfront.s3_bucket_name
}

output "frontend_s3_bucket_name" {
  description = "S3 bucket name for frontend (alias for workflow compatibility)"
  value       = module.s3_cloudfront.s3_bucket_name
}

output "frontend_url" {
  description = "Frontend URL (CloudFront or custom domain)"
  value       = var.domain_name != "" ? "https://${var.domain_name}" : "https://${module.s3_cloudfront.cloudfront_distribution_domain}"
}

# Summary Output
output "deployment_summary" {
  description = "Summary of deployed resources"
  value = {
    environment       = var.environment
    region            = var.aws_region
    identity_service  = module.app_runner.identity_service_url
    api_gateway       = module.app_runner.api_gateway_url
    data_service      = module.app_runner.data_service_url
    frontend          = var.domain_name != "" ? "https://${var.domain_name}" : "https://${module.s3_cloudfront.cloudfront_distribution_domain}"
    database_endpoint = module.rds.endpoint
  }
}






