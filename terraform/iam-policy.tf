# IAM Policy for Terraform Template Deployment
# Organized by resource groups for better maintainability

terraform {
  required_version = ">= 1.5.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "us-east-1"
}

variable "policy_name" {
  description = "Name of the IAM policy"
  type        = string
  default     = "TerraformTemplatePolicy"
}

# Core - STS Identity
data "aws_iam_policy_document" "core_identity" {
  statement {
    sid    = "CoreIdentity"
    effect = "Allow"
    actions = [
      "sts:GetCallerIdentity"
    ]
    resources = ["*"]
  }
}

# Terraform State Management - S3
data "aws_iam_policy_document" "terraform_state_s3" {
  statement {
    sid    = "TerraformStateS3"
    effect = "Allow"
    actions = [
      "s3:CreateBucket",
      "s3:DeleteBucket",
      "s3:GetBucketVersioning",
      "s3:PutBucketVersioning",
      "s3:GetEncryptionConfiguration",
      "s3:PutEncryptionConfiguration",
      "s3:GetBucketPublicAccessBlock",
      "s3:PutBucketPublicAccessBlock",
      "s3:GetLifecycleConfiguration",
      "s3:PutLifecycleConfiguration",
      "s3:GetBucketTagging",
      "s3:PutBucketTagging",
      "s3:ListBucket",
      "s3:GetObject",
      "s3:PutObject",
      "s3:DeleteObject"
    ]
    resources = [
      "arn:aws:s3:::*-terraform-state-*",
      "arn:aws:s3:::*-terraform-state-*/*"
    ]
  }
}

# Terraform State Management - DynamoDB
data "aws_iam_policy_document" "terraform_state_dynamodb" {
  statement {
    sid    = "TerraformStateDynamoDB"
    effect = "Allow"
    actions = [
      "dynamodb:CreateTable",
      "dynamodb:DeleteTable",
      "dynamodb:DescribeTable",
      "dynamodb:GetItem",
      "dynamodb:PutItem",
      "dynamodb:DeleteItem",
      "dynamodb:TagResource",
      "dynamodb:ListTagsOfResource"
    ]
    resources = ["arn:aws:dynamodb:*:*:table/*-terraform-locks"]
  }
}

# Networking - VPC, Subnets, Internet Gateway, Route Tables, Security Groups
data "aws_iam_policy_document" "networking" {
  statement {
    sid    = "NetworkingAll"
    effect = "Allow"
    actions = [
      # VPC
      "ec2:DescribeAvailabilityZones",
      "ec2:DescribeAccountAttributes",
      "ec2:DescribeVpcs",
      "ec2:CreateVpc",
      "ec2:DeleteVpc",
      "ec2:ModifyVpcAttribute",
      "ec2:DescribeVpcAttribute",
      # Subnets
      "ec2:DescribeSubnets",
      "ec2:CreateSubnet",
      "ec2:DeleteSubnet",
      "ec2:ModifySubnetAttribute",
      # Internet Gateway
      "ec2:DescribeInternetGateways",
      "ec2:CreateInternetGateway",
      "ec2:DeleteInternetGateway",
      "ec2:AttachInternetGateway",
      "ec2:DetachInternetGateway",
      # Route Tables
      "ec2:DescribeRouteTables",
      "ec2:CreateRouteTable",
      "ec2:DeleteRouteTable",
      "ec2:CreateRoute",
      "ec2:DeleteRoute",
      "ec2:AssociateRouteTable",
      "ec2:DisassociateRouteTable",
      "ec2:ReplaceRouteTableAssociation",
      # Security Groups
      "ec2:DescribeSecurityGroups",
      "ec2:CreateSecurityGroup",
      "ec2:DeleteSecurityGroup",
      "ec2:AuthorizeSecurityGroupIngress",
      "ec2:AuthorizeSecurityGroupEgress",
      "ec2:RevokeSecurityGroupIngress",
      "ec2:RevokeSecurityGroupEgress",
      "ec2:ModifySecurityGroupRules",
      # Tags
      "ec2:CreateTags",
      "ec2:DeleteTags",
      "ec2:DescribeTags"
    ]
    resources = ["*"]
  }
}

# Container Registry - ECR
data "aws_iam_policy_document" "container_registry" {
  statement {
    sid    = "ContainerRegistryECR"
    effect = "Allow"
    actions = [
      "ecr:CreateRepository",
      "ecr:DeleteRepository",
      "ecr:DescribeRepositories",
      "ecr:GetRepositoryPolicy",
      "ecr:SetRepositoryPolicy",
      "ecr:DeleteRepositoryPolicy",
      "ecr:PutLifecyclePolicy",
      "ecr:GetLifecyclePolicy",
      "ecr:DeleteLifecyclePolicy",
      "ecr:TagResource",
      "ecr:UntagResource",
      "ecr:ListTagsForResource",
      "ecr:PutImageScanningConfiguration",
      "ecr:GetAuthorizationToken",
      "ecr:BatchGetImage",
      "ecr:BatchCheckLayerAvailability",
      "ecr:CompleteLayerUpload",
      "ecr:InitiateLayerUpload",
      "ecr:PutImage",
      "ecr:UploadLayerPart"
    ]
    resources = ["*"]
  }
}

# Authentication - Cognito
data "aws_iam_policy_document" "authentication" {
  statement {
    sid    = "AuthenticationCognito"
    effect = "Allow"
    actions = [
      "cognito-idp:CreateUserPool",
      "cognito-idp:DeleteUserPool",
      "cognito-idp:DescribeUserPool",
      "cognito-idp:UpdateUserPool",
      "cognito-idp:CreateUserPoolClient",
      "cognito-idp:DeleteUserPoolClient",
      "cognito-idp:DescribeUserPoolClient",
      "cognito-idp:UpdateUserPoolClient",
      "cognito-idp:CreateUserPoolDomain",
      "cognito-idp:DeleteUserPoolDomain",
      "cognito-idp:DescribeUserPoolDomain",
      "cognito-idp:ListUserPools",
      "cognito-idp:ListUserPoolClients",
      "cognito-idp:TagResource",
      "cognito-idp:UntagResource",
      "cognito-idp:ListTagsForResource"
    ]
    resources = ["*"]
  }
}

# Logging - CloudWatch
data "aws_iam_policy_document" "logging" {
  statement {
    sid    = "LoggingCloudWatch"
    effect = "Allow"
    actions = [
      "logs:CreateLogGroup",
      "logs:DeleteLogGroup",
      "logs:DescribeLogGroups",
      "logs:PutRetentionPolicy",
      "logs:DeleteRetentionPolicy",
      "logs:TagResource",
      "logs:UntagResource",
      "logs:ListTagsForResource"
    ]
    resources = ["arn:aws:logs:*:*:log-group:/aws/apprunner/*"]
  }
}

# Cost Management - Budgets and Anomaly Detection
data "aws_iam_policy_document" "cost_management" {
  statement {
    sid    = "CostManagementBudgets"
    effect = "Allow"
    actions = [
      "budgets:CreateBudget",
      "budgets:DeleteBudget",
      "budgets:DescribeBudget",
      "budgets:ModifyBudget",
      "budgets:ViewBudget"
    ]
    resources = ["*"]
  }

  statement {
    sid    = "CostManagementAnomalyDetection"
    effect = "Allow"
    actions = [
      "ce:CreateAnomalyMonitor",
      "ce:DeleteAnomalyMonitor",
      "ce:GetAnomalyMonitors",
      "ce:UpdateAnomalyMonitor",
      "ce:CreateAnomalySubscription",
      "ce:DeleteAnomalySubscription",
      "ce:GetAnomalySubscriptions",
      "ce:UpdateAnomalySubscription",
      "ce:TagResource",
      "ce:UntagResource"
    ]
    resources = ["*"]
  }
}

# Database - RDS and Secrets Manager
data "aws_iam_policy_document" "database" {
  statement {
    sid    = "DatabaseRDS"
    effect = "Allow"
    actions = [
      "rds:CreateDBInstance",
      "rds:DeleteDBInstance",
      "rds:DescribeDBInstances",
      "rds:ModifyDBInstance",
      "rds:CreateDBSubnetGroup",
      "rds:DeleteDBSubnetGroup",
      "rds:DescribeDBSubnetGroups",
      "rds:CreateDBParameterGroup",
      "rds:DeleteDBParameterGroup",
      "rds:DescribeDBParameterGroups",
      "rds:ModifyDBParameterGroup",
      "rds:DescribeDBParameters",
      "rds:AddTagsToResource",
      "rds:RemoveTagsFromResource",
      "rds:ListTagsForResource",
      "rds:CreateDBSnapshot",
      "rds:DeleteDBSnapshot",
      "rds:DescribeDBSnapshots"
    ]
    resources = ["*"]
  }

  statement {
    sid    = "DatabaseSecretsManager"
    effect = "Allow"
    actions = [
      "secretsmanager:CreateSecret",
      "secretsmanager:DeleteSecret",
      "secretsmanager:DescribeSecret",
      "secretsmanager:GetSecretValue",
      "secretsmanager:PutSecretValue",
      "secretsmanager:UpdateSecret",
      "secretsmanager:TagResource",
      "secretsmanager:UntagResource"
    ]
    resources = ["arn:aws:secretsmanager:*:*:secret:*"]
  }
}

# Compute - App Runner
data "aws_iam_policy_document" "compute" {
  statement {
    sid    = "ComputeAppRunner"
    effect = "Allow"
    actions = [
      "apprunner:CreateService",
      "apprunner:DeleteService",
      "apprunner:DescribeService",
      "apprunner:UpdateService",
      "apprunner:ListServices",
      "apprunner:CreateVpcConnector",
      "apprunner:DeleteVpcConnector",
      "apprunner:DescribeVpcConnector",
      "apprunner:ListVpcConnectors",
      "apprunner:TagResource",
      "apprunner:UntagResource",
      "apprunner:ListTagsForResource"
    ]
    resources = ["*"]
  }
}

# IAM - Roles for App Runner
data "aws_iam_policy_document" "iam_roles" {
  statement {
    sid    = "IAMRolesForAppRunner"
    effect = "Allow"
    actions = [
      "iam:CreateRole",
      "iam:DeleteRole",
      "iam:GetRole",
      "iam:PassRole",
      "iam:CreatePolicy",
      "iam:DeletePolicy",
      "iam:GetPolicy",
      "iam:AttachRolePolicy",
      "iam:DetachRolePolicy",
      "iam:ListAttachedRolePolicies",
      "iam:TagRole",
      "iam:UntagRole",
      "iam:TagPolicy",
      "iam:UntagPolicy"
    ]
    resources = [
      "arn:aws:iam::*:role/*-app-runner-*",
      "arn:aws:iam::*:policy/*-app-runner-*"
    ]
  }
}

# CDN - CloudFront
data "aws_iam_policy_document" "cdn" {
  statement {
    sid    = "CDNCloudFront"
    effect = "Allow"
    actions = [
      "cloudfront:CreateDistribution",
      "cloudfront:DeleteDistribution",
      "cloudfront:GetDistribution",
      "cloudfront:UpdateDistribution",
      "cloudfront:TagResource",
      "cloudfront:UntagResource",
      "cloudfront:CreateOriginAccessControl",
      "cloudfront:DeleteOriginAccessControl",
      "cloudfront:GetOriginAccessControl",
      "cloudfront:CreateInvalidation"
    ]
    resources = ["*"]
  }
}

# Frontend - S3 Buckets
data "aws_iam_policy_document" "frontend" {
  statement {
    sid    = "FrontendS3Buckets"
    effect = "Allow"
    actions = [
      "s3:CreateBucket",
      "s3:DeleteBucket",
      "s3:GetBucket*",
      "s3:PutBucket*",
      "s3:ListBucket",
      "s3:GetObject",
      "s3:PutObject",
      "s3:DeleteObject"
    ]
    resources = [
      "arn:aws:s3:::*-frontend",
      "arn:aws:s3:::*-frontend/*"
    ]
  }
}

# Combine all policy documents
data "aws_iam_policy_document" "combined" {
  source_policy_documents = [
    data.aws_iam_policy_document.core_identity.json,
    data.aws_iam_policy_document.terraform_state_s3.json,
    data.aws_iam_policy_document.terraform_state_dynamodb.json,
    data.aws_iam_policy_document.networking.json,
    data.aws_iam_policy_document.container_registry.json,
    data.aws_iam_policy_document.authentication.json,
    data.aws_iam_policy_document.logging.json,
    data.aws_iam_policy_document.cost_management.json,
    data.aws_iam_policy_document.database.json,
    data.aws_iam_policy_document.compute.json,
    data.aws_iam_policy_document.iam_roles.json,
    data.aws_iam_policy_document.cdn.json,
    data.aws_iam_policy_document.frontend.json
  ]
}

# Create the IAM policy
resource "aws_iam_policy" "terraform_template" {
  name        = var.policy_name
  description = "Permissions for Sri Template Terraform deployment - organized by resource groups"
  policy      = data.aws_iam_policy_document.combined.json

  tags = {
    Name        = var.policy_name
    ManagedBy   = "Terraform"
    Purpose     = "Template Infrastructure Deployment"
    Environment = "all"
  }
}

# Output the policy ARN
output "policy_arn" {
  description = "ARN of the created IAM policy"
  value       = aws_iam_policy.terraform_template.arn
}

output "policy_name" {
  description = "Name of the created IAM policy"
  value       = aws_iam_policy.terraform_template.name
}

output "policy_json" {
  description = "JSON policy document (for review)"
  value       = data.aws_iam_policy_document.combined.json
}

output "attach_to_user_command" {
  description = "Command to attach policy to a user"
  value       = "aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn ${aws_iam_policy.terraform_template.arn}"
}






