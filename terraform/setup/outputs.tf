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

# Shared resources from sri-template (reference only, not created)
output "vpc_id" {
  description = "VPC ID (shared with sri-template)"
  value       = data.aws_vpc.existing.id
}

output "public_subnet_ids" {
  description = "Public subnet IDs (shared with sri-template)"
  value       = data.aws_subnets.existing_public.ids
}

output "app_runner_security_group_id" {
  description = "App Runner security group ID (shared with sri-template)"
  value       = data.aws_security_group.existing_app_runner.id
}

output "rds_security_group_id" {
  description = "RDS security group ID (shared with sri-template)"
  value       = data.aws_security_group.existing_rds.id
}

output "cognito_user_pool_id" {
  description = "Cognito User Pool ID (shared with sri-template)"
  value       = tolist(data.aws_cognito_user_pools.existing.ids)[0]
}

output "cognito_user_pool_client_id" {
  description = "Cognito User Pool Client ID (sri-subscription specific)"
  value       = aws_cognito_user_pool_client.sri_subscription.id
}

output "cognito_domain" {
  description = "Cognito domain (shared with sri-template)"
  value       = "sri-test-project-1-1zvox1e6"
}





