# Development Environment Configuration for navarch-studio

# Project Configuration
project_name = "navarch-studio"
environment  = "dev"
aws_region   = "us-east-1"
cost_center  = "engineering"

# Backend Configuration
# NOTE: Update these values after running terraform/setup
s3_bucket_name      = "navarch-studio-terraform-state-{ACCOUNT_ID}"
dynamodb_table_name = "navarch-studio-terraform-locks"

# Infrastructure from terraform/setup (UPDATE AFTER SETUP)
# Run: terraform output -state=../setup/terraform.tfstate
vpc_id                       = "UPDATE_AFTER_SETUP"
public_subnet_ids            = ["UPDATE_AFTER_SETUP"]
app_runner_security_group_id = "UPDATE_AFTER_SETUP"
rds_security_group_id        = "UPDATE_AFTER_SETUP"

# ECR Repository URLs (UPDATE AFTER SETUP)
ecr_repository_urls = {
  identity_service = "{ACCOUNT_ID}.dkr.ecr.us-east-1.amazonaws.com/navarch-studio-identity-service"
  api_gateway      = "{ACCOUNT_ID}.dkr.ecr.us-east-1.amazonaws.com/navarch-studio-api-gateway"
  data_service     = "{ACCOUNT_ID}.dkr.ecr.us-east-1.amazonaws.com/navarch-studio-data-service"
  frontend         = "{ACCOUNT_ID}.dkr.ecr.us-east-1.amazonaws.com/navarch-studio-frontend"
}

# Cognito Configuration (UPDATE AFTER SETUP)
cognito_user_pool_id        = "UPDATE_AFTER_SETUP"
cognito_user_pool_client_id = "UPDATE_AFTER_SETUP"
cognito_domain              = "UPDATE_AFTER_SETUP"

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
