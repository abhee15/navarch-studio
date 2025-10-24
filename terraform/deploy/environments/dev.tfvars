# Development Environment Configuration for sri-subscription

# Project Configuration
project_name = "sri-subscription"
environment  = "dev"
aws_region   = "us-east-1"
cost_center  = "engineering"

# Backend Configuration
s3_bucket_name      = "sri-subscription-terraform-state-344870914438"
dynamodb_table_name = "sri-subscription-terraform-locks"

# Shared Infrastructure from sri-template (from terraform/setup outputs)
vpc_id                       = "vpc-0d6ade52f01dbdedd"
public_subnet_ids            = ["subnet-03e80ad7d95b17740", "subnet-0bb9e7e3a7909e470"]
app_runner_security_group_id = "sg-0d5975d8e0d46bf85"
rds_security_group_id        = "sg-08de6e4bc300fdd2d"

# ECR Repository URLs
ecr_repository_urls = {
  identity_service = "344870914438.dkr.ecr.us-east-1.amazonaws.com/sri-subscription-identity-service"
  api_gateway      = "344870914438.dkr.ecr.us-east-1.amazonaws.com/sri-subscription-api-gateway"
  data_service     = "344870914438.dkr.ecr.us-east-1.amazonaws.com/sri-subscription-data-service"
  frontend         = "344870914438.dkr.ecr.us-east-1.amazonaws.com/sri-subscription-frontend"
}

# Cognito Configuration (shared with sri-template)
cognito_user_pool_id        = "us-east-1_WTfHVTfHT"
cognito_user_pool_client_id = "79td1a61lt9c11f4lsaogbm6pe"
cognito_domain              = "sri-test-project-1-1zvox1e6"

# Database Configuration (cost-optimized for dev)
db_instance_class     = "db.t3.micro" # Free Tier eligible
db_allocated_storage  = 20            # Free Tier: 20GB
enable_multi_az       = false         # Single AZ for dev
backup_retention_days = 7             # Minimum retention

# App Runner Configuration (minimal resources for dev)
app_runner_cpu    = "256" # 0.25 vCPU
app_runner_memory = "512" # 512 MB

# Frontend Configuration
domain_name = "" # Use CloudFront domain for dev
