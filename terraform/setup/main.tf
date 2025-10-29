terraform {
  required_version = ">= 1.5.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }

  backend "s3" {
    # Backend configuration will be provided via command line or backend-config file
    # terraform init -backend-config=backend-config.tfvars
    # Or: terraform init -backend-config="bucket=..." -backend-config="key=..." -backend-config="region=..."
    key     = "setup/terraform.tfstate"
    encrypt = true
  }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = "NAV-${var.project_name}"
      Environment = "setup"
      ManagedBy   = "Terraform"
      CostCenter  = var.cost_center
    }
  }
}

# Data sources
data "aws_caller_identity" "current" {}
data "aws_availability_zones" "available" {
  state = "available"
}

# Note: Common tags are defined in provider default_tags block above
# No need for locals.common_tags
