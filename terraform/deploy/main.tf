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
    # Note: Account ID is hardcoded to avoid circular dependency with data.aws_caller_identity
    # This is the same bucket created by terraform/setup
    bucket = "navarch-studio-terraform-state-344870914438"
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
  prevent_destroy       = var.environment == "prod" # Prevent destruction in production
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
  # First apply: Leave empty (avoids circular dependency)
  # Second apply: Pass via -var="cloudfront_domain_override=domain" to update CORS
  cloudfront_distribution_domain = var.cloudfront_domain_override

  # Service sizing
  cpu    = var.app_runner_cpu
  memory = var.app_runner_memory

  # Benchmark buckets to inject into Data Service env
  benchmark_raw_bucket     = module.s3_benchmark.raw_bucket_name
  benchmark_curated_bucket = module.s3_benchmark.curated_bucket_name
}

# S3 & CloudFront Module
module "s3_cloudfront" {
  source = "./modules/s3-cloudfront"

  project_name = var.project_name
  environment  = var.environment
  domain_name  = var.domain_name
  aws_region   = var.aws_region

  # Runtime configuration for frontend
  api_gateway_url      = module.app_runner.api_gateway_url
  auth_mode            = "cognito"
  cognito_user_pool_id = data.terraform_remote_state.setup.outputs.cognito_user_pool_id
  cognito_client_id    = data.terraform_remote_state.setup.outputs.cognito_user_pool_client_id
}

# Benchmark S3 buckets
module "s3_benchmark" {
  source = "./modules/s3-benchmark"

  project_name = var.project_name
  environment  = var.environment
}

# IAM policy to allow Data Service to access benchmark buckets
resource "aws_iam_policy" "benchmark_s3_access" {
  name = "${var.project_name}-${var.environment}-benchmark-s3-access"

  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Effect = "Allow",
        Action = [
          "s3:PutObject",
          "s3:GetObject",
          "s3:DeleteObject",
          "s3:ListBucket"
        ],
        Resource = [
          "arn:aws:s3:::${module.s3_benchmark.raw_bucket_name}",
          "arn:aws:s3:::${module.s3_benchmark.raw_bucket_name}/*",
          "arn:aws:s3:::${module.s3_benchmark.curated_bucket_name}",
          "arn:aws:s3:::${module.s3_benchmark.curated_bucket_name}/*"
        ]
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "attach_benchmark_s3" {
  role       = module.app_runner.instance_role_name
  policy_arn = aws_iam_policy.benchmark_s3_access.arn
}
