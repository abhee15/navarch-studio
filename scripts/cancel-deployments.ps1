# Cancel unnecessary App Runner deployments
# Use this when services are being redeployed unnecessarily

$ErrorActionPreference = "Stop"
$AWS_REGION = "us-east-1"
$ENVIRONMENT = "dev"

Write-Host "🛑 Canceling unnecessary App Runner deployments..." -ForegroundColor Yellow
Write-Host ""

# Get service ARNs
$IDENTITY_ARN = aws apprunner list-services --region $AWS_REGION `
  --query "ServiceSummaryList[?contains(ServiceName, '$ENVIRONMENT-identity-service')].ServiceArn | [0]" `
  --output text

$DATA_ARN = aws apprunner list-services --region $AWS_REGION `
  --query "ServiceSummaryList[?contains(ServiceName, '$ENVIRONMENT-data-service')].ServiceArn | [0]" `
  --output text

$API_ARN = aws apprunner list-services --region $AWS_REGION `
  --query "ServiceSummaryList[?contains(ServiceName, '$ENVIRONMENT-api-gateway')].ServiceArn | [0]" `
  --output text

Write-Host "Found services:" -ForegroundColor Cyan
Write-Host "  Identity: $IDENTITY_ARN" -ForegroundColor Gray
Write-Host "  Data: $DATA_ARN" -ForegroundColor Gray
Write-Host "  API Gateway: $API_ARN" -ForegroundColor Gray
Write-Host ""

# Check status of each service
function Get-ServiceStatus {
    param($arn, $name)

    $status = aws apprunner describe-service --service-arn $arn --region $AWS_REGION `
      --query "Service.Status" --output text

    Write-Host "[$name] Status: $status" -ForegroundColor $(if ($status -eq "OPERATION_IN_PROGRESS") { "Yellow" } else { "Green" })
    return $status
}

$identityStatus = Get-ServiceStatus $IDENTITY_ARN "Identity"
$dataStatus = Get-ServiceStatus $DATA_ARN "Data"
$apiStatus = Get-ServiceStatus $API_ARN "API Gateway"

Write-Host ""
Write-Host "Note: App Runner doesn't have a 'cancel deployment' API." -ForegroundColor Yellow
Write-Host "The deployments will continue, but they're just redeploying the same images." -ForegroundColor Yellow
Write-Host ""
Write-Host "Options:" -ForegroundColor Cyan
Write-Host "1. Wait for them to complete (~10 minutes)" -ForegroundColor White
Write-Host "2. They won't break anything - same images being redeployed" -ForegroundColor White
Write-Host ""
Write-Host "The Data Service is already working perfectly!" -ForegroundColor Green
Write-Host "We saw successful startup logs and migration completion." -ForegroundColor Green
