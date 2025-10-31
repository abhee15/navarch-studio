# Random password for RDS
resource "random_password" "db_password" {
  length           = 32
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?" # Exclude /, @, ", space (not allowed by RDS)

  # Prevent regeneration unless these values change
  # This prevents RDS from being recreated due to password regeneration
  keepers = {
    environment = var.environment
    project     = var.project_name
  }
}

# Store password in Secrets Manager
resource "aws_secretsmanager_secret" "db_password" {
  name = "${var.project_name}-${var.environment}-db-password"

  # For non-prod environments, allow immediate deletion and recreation
  # For prod, keep the default 30-day recovery window
  recovery_window_in_days = var.environment == "prod" ? 30 : 0

  tags = {
    Name = "${var.project_name}-${var.environment}-db-password"
  }
}

resource "aws_secretsmanager_secret_version" "db_password" {
  secret_id = aws_secretsmanager_secret.db_password.id
  secret_string = jsonencode({
    username = "postgres"
    password = random_password.db_password.result
    engine   = "postgres"
    host     = aws_db_instance.main.address
    port     = aws_db_instance.main.port
    dbname   = aws_db_instance.main.db_name
  })
}

# DB Subnet Group
resource "aws_db_subnet_group" "main" {
  name       = "${var.project_name}-${var.environment}-db-subnet-group"
  subnet_ids = var.subnet_ids

  tags = {
    Name = "${var.project_name}-${var.environment}-db-subnet-group"
  }
}

# RDS PostgreSQL Instance
resource "aws_db_instance" "main" {
  identifier        = "${var.project_name}-${var.environment}-postgres"
  engine            = "postgres"
  engine_version    = "15"
  instance_class    = var.instance_class
  allocated_storage = var.allocated_storage
  storage_type      = "gp3" # General Purpose SSD (gp3) is cheaper than gp2
  storage_encrypted = true

  db_name  = replace("${var.project_name}_${var.environment}", "-", "_")
  username = "postgres"
  password = random_password.db_password.result

  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = var.security_group_ids
  publicly_accessible    = true # Required for App Runner DEFAULT egress (no NAT Gateway)

  multi_az                = var.multi_az
  backup_retention_period = var.backup_retention_days
  backup_window           = "03:00-04:00"
  maintenance_window      = "mon:04:00-mon:05:00"

  # Snapshots and deletion protection
  skip_final_snapshot       = var.environment != "prod" # Keep final snapshot for prod
  final_snapshot_identifier = var.environment == "prod" ? "${var.project_name}-${var.environment}-final-snapshot-${formatdate("YYYY-MM-DD-hhmm", timestamp())}" : null
  deletion_protection       = var.environment == "prod" # Protect prod from accidental deletion

  # Performance Insights (optional, adds cost)
  enabled_cloudwatch_logs_exports = ["postgresql", "upgrade"]

  # Auto minor version upgrades
  auto_minor_version_upgrade = true

  # Lifecycle management to prevent unnecessary recreation
  lifecycle {
    # Prevent accidental destruction in production
    prevent_destroy = var.environment == "prod"

    # Ignore password changes - password is managed via Secrets Manager rotation
    # Without this, Terraform would try to recreate RDS if password changes
    ignore_changes = [password]
  }

  tags = {
    Name = "${var.project_name}-${var.environment}-postgres"
  }
}
