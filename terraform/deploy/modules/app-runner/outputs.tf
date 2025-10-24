output "vpc_connector_arn" {
  description = "VPC Connector ARN"
  value       = aws_apprunner_vpc_connector.main.arn
}

output "identity_service_url" {
  description = "Identity Service URL"
  value       = "https://${aws_apprunner_service.identity_service.service_url}"
}

output "identity_service_arn" {
  description = "Identity Service ARN"
  value       = aws_apprunner_service.identity_service.arn
}

output "api_gateway_url" {
  description = "API Gateway URL"
  value       = "https://${aws_apprunner_service.api_gateway.service_url}"
}

output "api_gateway_arn" {
  description = "API Gateway ARN"
  value       = aws_apprunner_service.api_gateway.arn
}

output "data_service_url" {
  description = "Data Service URL"
  value       = "https://${aws_apprunner_service.data_service.service_url}"
}

output "data_service_arn" {
  description = "Data Service ARN"
  value       = aws_apprunner_service.data_service.arn
}






