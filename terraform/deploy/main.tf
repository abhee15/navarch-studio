terraform {
  required_version = ">= 1.5.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  backend "s3" {
    # Backend configuration will be provided via backend-config file
    # terraform init -backend-config=backend-config.tfvars
    key     = "deploy/terraform.tfstate"
    encrypt = true
  }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = "NAV-${var.project_name}"
      Environment = var.environment
      ManagedBy   = "Terraform"
      CostCenter  = var.cost_center
    }
  }
}

# Data sources
data "aws_caller_identity" "current" {}

# Reference setup infrastructure (single source of truth)
data "terraform_remote_state" "setup" {
  backend = "s3"
  config = {
    bucket = "${var.project_name}-terraform-state-${data.aws_caller_identity.current.account_id}"
    key    = "setup/terraform.tfstate"
    region = var.aws_region
  }
}

# RDS Module
module "rds" {
  source = "./modules/rds"

  project_name       = var.project_name
  environment        = var.environment
  vpc_id             = data.terraform_remote_state.setup.outputs.vpc_id
  subnet_ids         = data.terraform_remote_state.setup.outputs.public_subnet_ids
  security_group_ids = [data.terraform_remote_state.setup.outputs.rds_security_group_id]

  instance_class        = var.db_instance_class
  allocated_storage     = var.db_allocated_storage
  multi_az              = var.enable_multi_az
  backup_retention_days = var.backup_retention_days
  publicly_accessible   = false # Not publicly accessible despite being in public subnet
}

# App Runner Module
module "app_runner" {
  source = "./modules/app-runner"

  project_name       = var.project_name
  environment        = var.environment
  vpc_id             = data.terraform_remote_state.setup.outputs.vpc_id
  subnet_ids         = data.terraform_remote_state.setup.outputs.public_subnet_ids
  security_group_ids = [data.terraform_remote_state.setup.outputs.app_runner_security_group_id]

  ecr_repository_urls = data.terraform_remote_state.setup.outputs.ecr_repository_urls

  # Database connection
  rds_endpoint = module.rds.endpoint
  rds_port     = module.rds.port
  rds_database = module.rds.database_name
  rds_username = module.rds.username
  rds_password = module.rds.password

  # Cognito configuration
  cognito_user_pool_id        = data.terraform_remote_state.setup.outputs.cognito_user_pool_id
  cognito_user_pool_client_id = data.terraform_remote_state.setup.outputs.cognito_user_pool_client_id
  cognito_domain              = data.terraform_remote_state.setup.outputs.cognito_domain

  # CORS - CloudFront URL for browser-based requests
  # NOTE: Leave empty on first deployment to avoid circular dependency
  # After first deployment, you can manually set this via AWS Console or redeploy
  # cloudfront_distribution_domain = "https://d123abc456.cloudfront.net"
  cloudfront_distribution_domain = ""

  # Service sizing
  cpu    = var.app_runner_cpu
  memory = var.app_runner_memory
}

# S3 & CloudFront Module
module "s3_cloudfront" {
  source = "./modules/s3-cloudfront"

  project_name = var.project_name
  environment  = var.environment
  domain_name  = var.domain_name

  # API Gateway URL for frontend config
  api_gateway_url = module.app_runner.api_gateway_url
}
