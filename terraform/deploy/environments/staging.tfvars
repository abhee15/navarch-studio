# Staging Environment Configuration

environment = "staging"

# Database Configuration (slightly more robust than dev)
db_instance_class     = "db.t3.small" # Better performance than micro
db_allocated_storage  = 50            # More storage
enable_multi_az       = false         # Still single AZ to save costs
backup_retention_days = 14            # Longer retention

# App Runner Configuration (more resources)
app_runner_cpu    = "512"  # 0.5 vCPU
app_runner_memory = "1024" # 1 GB

# Frontend Configuration
domain_name = "" # Update with staging domain if available

