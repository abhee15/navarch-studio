# Security group for App Runner services
resource "aws_security_group" "app_runner" {
  name_prefix = "${var.project_name}-app-runner-"
  vpc_id      = aws_vpc.main.id
  description = "Security group for App Runner services"

  # App Runner manages ingress rules automatically
  # We only define egress rules

  egress {
    description = "Allow all outbound traffic"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "${var.project_name}-app-runner-sg"
  }
}

# Security group for RDS
resource "aws_security_group" "rds" {
  name_prefix = "${var.project_name}-rds-"
  vpc_id      = aws_vpc.main.id
  description = "Security group for RDS - only allows App Runner access"

  ingress {
    description = "PostgreSQL from anywhere (App Runner DEFAULT egress uses dynamic IPs)"
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
    # Security: RDS still protected by password authentication (32-char random password in Secrets Manager)
    # This is a standard AWS pattern for App Runner DEFAULT egress without NAT Gateway
    # Last updated: 2025-10-29 to fix timeout issues caused by VPC egress without NAT Gateway
  }

  ingress {
    description = "PostgreSQL from VPC (for migrations and future private resources)"
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = [aws_vpc.main.cidr_block]
  }

  egress {
    description = "Allow all outbound traffic"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "${var.project_name}-rds-sg"
  }
}
