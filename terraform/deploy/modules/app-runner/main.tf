# Get current AWS region
data "aws_region" "current" {}

# Observability Configuration for CloudWatch Logs
resource "aws_apprunner_observability_configuration" "main" {
  observability_configuration_name = "${var.project_name}-${var.environment}-observability"

  trace_configuration {
    vendor = "AWSXRAY"
  }

  tags = {
    Name = "${var.project_name}-${var.environment}-observability"
  }
}

# VPC Connector for App Runner
resource "aws_apprunner_vpc_connector" "main" {
  vpc_connector_name = "${var.project_name}-${var.environment}-vpc-connector"
  subnets            = var.subnet_ids
  security_groups    = var.security_group_ids

  tags = {
    Name = "${var.project_name}-${var.environment}-vpc-connector"
  }
}

# IAM Role for App Runner to access ECR
resource "aws_iam_role" "app_runner_ecr" {
  name = "${var.project_name}-${var.environment}-app-runner-ecr-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "build.apprunner.amazonaws.com"
      }
    }]
  })

  tags = {
    Name = "${var.project_name}-${var.environment}-app-runner-ecr-role"
  }
}

resource "aws_iam_role_policy_attachment" "app_runner_ecr" {
  role       = aws_iam_role.app_runner_ecr.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSAppRunnerServicePolicyForECRAccess"
}

# IAM Role for App Runner instance (for accessing Secrets Manager, etc.)
resource "aws_iam_role" "app_runner_instance" {
  name = "${var.project_name}-${var.environment}-app-runner-instance-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "tasks.apprunner.amazonaws.com"
      }
    }]
  })

  tags = {
    Name = "${var.project_name}-${var.environment}-app-runner-instance-role"
  }
}

# Policy for Secrets Manager access
resource "aws_iam_policy" "secrets_access" {
  name = "${var.project_name}-${var.environment}-secrets-access"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = [
        "secretsmanager:GetSecretValue",
        "secretsmanager:DescribeSecret"
      ]
      Resource = "arn:aws:secretsmanager:*:*:secret:${var.project_name}-${var.environment}-*"
    }]
  })

  tags = {
    Name = "${var.project_name}-${var.environment}-secrets-access"
  }
}

resource "aws_iam_role_policy_attachment" "secrets_access" {
  role       = aws_iam_role.app_runner_instance.name
  policy_arn = aws_iam_policy.secrets_access.arn
}

# Identity Service
resource "aws_apprunner_service" "identity_service" {
  service_name = "${var.project_name}-${var.environment}-identity-service"

  source_configuration {
    authentication_configuration {
      access_role_arn = aws_iam_role.app_runner_ecr.arn
    }

    image_repository {
      image_identifier      = "${var.ecr_repository_urls.identity_service}:latest"
      image_repository_type = "ECR"

      image_configuration {
        port = "8080"

        runtime_environment_variables = merge({
          # Use Staging for dev/staging so auto-migrations run, Production for prod
          ASPNETCORE_ENVIRONMENT               = var.environment == "prod" ? "Production" : "Staging"
          ConnectionStrings__DefaultConnection = "Host=${var.rds_endpoint};Port=${var.rds_port};Database=${var.rds_database};Username=${var.rds_username};Password=${var.rds_password}"
          Cognito__UserPoolId                  = var.cognito_user_pool_id
          Cognito__AppClientId                 = var.cognito_user_pool_client_id
          Cognito__Domain                      = var.cognito_domain
          Cognito__Region                      = data.aws_region.current.name
        },
        (var.benchmark_raw_bucket != "" && var.benchmark_curated_bucket != "") ? {
          Benchmark__RawBucket     = var.benchmark_raw_bucket
          Benchmark__CuratedBucket = var.benchmark_curated_bucket
        } : {})
      }
    }

    auto_deployments_enabled = false # Manual deployment
  }

  instance_configuration {
    cpu               = var.cpu
    memory            = var.memory
    instance_role_arn = aws_iam_role.app_runner_instance.arn
  }

  # Identity Service uses DEFAULT egress to access both RDS and Cognito JWKS endpoint
  # VPC egress was causing timeouts because it blocked internet access (no NAT Gateway)
  # Last updated: 2025-10-29 to fix timeout issues
  network_configuration {
    egress_configuration {
      egress_type = "DEFAULT"
    }
  }

  health_check_configuration {
    protocol            = "HTTP"
    path                = "/health"
    interval            = 10
    timeout             = 20 # Maximum allowed by AWS App Runner (1-20 seconds)
    healthy_threshold   = 1
    unhealthy_threshold = 5
  }

  observability_configuration {
    observability_enabled           = true
    observability_configuration_arn = aws_apprunner_observability_configuration.main.arn
  }

  tags = {
    Name = "${var.project_name}-${var.environment}-identity-service"
  }
}

# Data Service
resource "aws_apprunner_service" "data_service" {
  service_name = "${var.project_name}-${var.environment}-data-service"

  source_configuration {
    authentication_configuration {
      access_role_arn = aws_iam_role.app_runner_ecr.arn
    }

    image_repository {
      image_identifier      = "${var.ecr_repository_urls.data_service}:latest"
      image_repository_type = "ECR"

      image_configuration {
        port = "8080"

        runtime_environment_variables = {
          # Use Staging for dev/staging so auto-migrations run, Production for prod
          ASPNETCORE_ENVIRONMENT               = var.environment == "prod" ? "Production" : "Staging"
          ConnectionStrings__DefaultConnection = "Host=${var.rds_endpoint};Port=${var.rds_port};Database=${var.rds_database};Username=${var.rds_username};Password=${var.rds_password}"
          Cognito__UserPoolId                  = var.cognito_user_pool_id
          Cognito__AppClientId                 = var.cognito_user_pool_client_id
          Cognito__Domain                      = var.cognito_domain
          Cognito__Region                      = data.aws_region.current.name
        }
      }
    }

    auto_deployments_enabled = false
  }

  instance_configuration {
    cpu               = var.cpu
    memory            = var.memory
    instance_role_arn = aws_iam_role.app_runner_instance.arn
  }

  # Data Service uses DEFAULT egress to access both RDS and Cognito JWKS endpoint
  # VPC egress was causing timeouts because it blocked internet access (no NAT Gateway)
  # Last updated: 2025-10-29 to fix timeout issues
  network_configuration {
    egress_configuration {
      egress_type = "DEFAULT"
    }
  }

  health_check_configuration {
    protocol            = "HTTP"
    path                = "/health"
    interval            = 10
    timeout             = 20 # Maximum allowed by AWS App Runner (1-20 seconds)
    healthy_threshold   = 1
    unhealthy_threshold = 5
  }

  observability_configuration {
    observability_enabled           = true
    observability_configuration_arn = aws_apprunner_observability_configuration.main.arn
  }

  tags = {
    Name = "${var.project_name}-${var.environment}-data-service"
  }
}

# API Gateway Service
resource "aws_apprunner_service" "api_gateway" {
  service_name = "${var.project_name}-${var.environment}-api-gateway"

  source_configuration {
    authentication_configuration {
      access_role_arn = aws_iam_role.app_runner_ecr.arn
    }

    image_repository {
      image_identifier      = "${var.ecr_repository_urls.api_gateway}:latest"
      image_repository_type = "ECR"

      image_configuration {
        port = "8080"

        runtime_environment_variables = merge({
          # Use Staging for dev/staging so auto-migrations run, Production for prod
          ASPNETCORE_ENVIRONMENT    = var.environment == "prod" ? "Production" : "Staging"
          Services__IdentityService = "https://${aws_apprunner_service.identity_service.service_url}"
          Services__DataService     = "https://${aws_apprunner_service.data_service.service_url}"
          Cognito__UserPoolId       = var.cognito_user_pool_id
          Cognito__AppClientId      = var.cognito_user_pool_client_id
          Cognito__Domain           = var.cognito_domain
          Cognito__Region           = data.aws_region.current.name
          },
          # CORS - Add CloudFront origin (use index 10 to ADD to appsettings origins, not replace)
          var.cloudfront_distribution_domain != "" ? {
            Cors__AllowedOrigins__10 = "https://${var.cloudfront_distribution_domain}"
          } : {}
        )
      }
    }

    auto_deployments_enabled = false
  }

  instance_configuration {
    cpu               = var.cpu
    memory            = var.memory
    instance_role_arn = aws_iam_role.app_runner_instance.arn
  }

  # API Gateway uses DEFAULT egress (public internet) to reach AWS Cognito for JWT validation
  # This avoids the need for an expensive NAT Gateway while allowing Cognito access
  # Service-to-service calls use public HTTPS URLs (no VPC needed)
  # Last updated: 2025-10-28 to fix timeout issues
  network_configuration {
    egress_configuration {
      egress_type = "DEFAULT"
    }
  }

  health_check_configuration {
    protocol            = "HTTP"
    path                = "/health"
    interval            = 10
    timeout             = 20 # Maximum allowed by AWS App Runner (1-20 seconds)
    healthy_threshold   = 1
    unhealthy_threshold = 5
  }

  observability_configuration {
    observability_enabled           = true
    observability_configuration_arn = aws_apprunner_observability_configuration.main.arn
  }

  tags = {
    Name = "${var.project_name}-${var.environment}-api-gateway"
  }

  depends_on = [
    aws_apprunner_service.identity_service,
    aws_apprunner_service.data_service
  ]
}
