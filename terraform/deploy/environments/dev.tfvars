# Development Environment Configuration for navarch-studio

# Project Configuration
project_name = "navarch-studio"
environment  = "dev"
aws_region   = "us-east-1"
cost_center  = "engineering"

# NOTE: All setup infrastructure values (VPC, subnets, security groups, ECR, Cognito)
# are automatically pulled from terraform_remote_state - no manual configuration needed!
# This ensures single source of truth and automatic updates when setup changes.

# Database Configuration (cost-optimized for dev)
db_instance_class     = "db.t3.micro" # Free Tier eligible
db_allocated_storage  = 20            # Free Tier: 20GB
enable_multi_az       = false         # Single AZ for dev
backup_retention_days = 7             # Minimum retention

# App Runner Configuration (increased for compute-intensive operations)
# Note: Hydrostatic computations (curves, tables) require more resources
app_runner_cpu    = "1024" # 1.0 vCPU (was 256)
app_runner_memory = "2048" # 2 GB (was 512 MB)

# Frontend Configuration
domain_name = "" # Use CloudFront domain for dev
