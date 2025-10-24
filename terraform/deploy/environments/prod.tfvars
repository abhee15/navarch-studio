# Production Environment Configuration

environment = "prod"

# Database Configuration (production-grade)
db_instance_class     = "db.t3.medium" # Better performance for prod
db_allocated_storage  = 100            # More storage
enable_multi_az       = true           # High availability
backup_retention_days = 30             # Longer retention for compliance

# App Runner Configuration (production resources)
app_runner_cpu    = "1024" # 1 vCPU
app_runner_memory = "2048" # 2 GB

# Frontend Configuration
domain_name = "" # Update with production domain if available

