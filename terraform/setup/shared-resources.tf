# Data sources to reference existing sri-template shared infrastructure
# This allows sri-subscription to reuse VPC, Cognito, and Security Groups

# Reference existing Cognito User Pool from sri-template
data "aws_cognito_user_pools" "existing" {
  name = "sri-test-project-1-user-pool"
}

# Reference existing VPC from sri-template
data "aws_vpc" "existing" {
  filter {
    name   = "tag:Project"
    values = ["sri-test-project-1"]
  }
}

# Reference existing subnets
data "aws_subnets" "existing_public" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.existing.id]
  }
  filter {
    name   = "tag:Type"
    values = ["public"]
  }
}

# Reference existing security groups
data "aws_security_group" "existing_app_runner" {
  vpc_id = data.aws_vpc.existing.id
  filter {
    name   = "tag:Name"
    values = ["*app-runner*"]
  }
}

data "aws_security_group" "existing_rds" {
  vpc_id = data.aws_vpc.existing.id
  filter {
    name   = "tag:Name"
    values = ["*rds*"]
  }
}

# Output the shared resource IDs for use in deploy phase
output "shared_vpc_id" {
  description = "VPC ID shared with sri-template"
  value       = data.aws_vpc.existing.id
}

output "shared_subnet_ids" {
  description = "Subnet IDs shared with sri-template"
  value       = data.aws_subnets.existing_public.ids
}

output "shared_app_runner_sg_id" {
  description = "App Runner security group shared with sri-template"
  value       = data.aws_security_group.existing_app_runner.id
}

output "shared_rds_sg_id" {
  description = "RDS security group shared with sri-template"
  value       = data.aws_security_group.existing_rds.id
}

output "shared_cognito_user_pool_id" {
  description = "Cognito User Pool shared with sri-template"
  value       = tolist(data.aws_cognito_user_pools.existing.ids)[0]
}

