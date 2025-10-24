# CloudWatch log groups for services
resource "aws_cloudwatch_log_group" "identity_service" {
  name              = "/aws/apprunner/${var.project_name}-identity-service"
  retention_in_days = 7 # Reduced from 14 to save costs

  tags = {
    Name = "${var.project_name}-identity-service-logs"
  }
}

resource "aws_cloudwatch_log_group" "api_gateway" {
  name              = "/aws/apprunner/${var.project_name}-api-gateway"
  retention_in_days = 7 # Reduced from 14 to save costs

  tags = {
    Name = "${var.project_name}-api-gateway-logs"
  }
}

resource "aws_cloudwatch_log_group" "data_service" {
  name              = "/aws/apprunner/${var.project_name}-data-service"
  retention_in_days = 7 # Reduced from 14 to save costs

  tags = {
    Name = "${var.project_name}-data-service-logs"
  }
}





