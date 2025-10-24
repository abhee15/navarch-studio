# Development Environment Configuration

environment = "dev"

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

