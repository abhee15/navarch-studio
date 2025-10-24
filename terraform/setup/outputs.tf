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





