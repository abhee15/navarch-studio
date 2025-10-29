# Manual CORS Fix for API Gateway
# Run this to add CloudFront domain to API Gateway's allowed origins

$ErrorActionPreference = "Stop"
$AWS_REGION = "us-east-1"
$ENVIRONMENT = "dev"

Write-Host "🔧 Fixing CORS for API Gateway..." -ForegroundColor Cyan
Write-Host ""

# Get CloudFront domain
Write-Host "1. Getting CloudFront domain..." -ForegroundColor Yellow
cd terraform/deploy
$CF_DOMAIN = terraform output -raw cloudfront_domain_name
cd ../..

if ([string]::IsNullOrEmpty($CF_DOMAIN)) {
    Write-Host "❌ Could not get CloudFront domain from Terraform" -ForegroundColor Red
    exit 1
}

$CF_URL = "https://$CF_DOMAIN"
Write-Host "   CloudFront URL: $CF_URL" -ForegroundColor Green
Write-Host ""

# Get API Gateway service ARN
Write-Host "2. Finding API Gateway service..." -ForegroundColor Yellow
$API_ARN = aws apprunner list-services --region $AWS_REGION `
    --query "ServiceSummaryList[?contains(ServiceName, '$ENVIRONMENT-api-gateway')].ServiceArn | [0]" `
    --output text

if ([string]::IsNullOrEmpty($API_ARN) -or $API_ARN -eq "None") {
    Write-Host "❌ Could not find API Gateway service" -ForegroundColor Red
    exit 1
}

Write-Host "   Service ARN: $API_ARN" -ForegroundColor Green
Write-Host ""

# Get current configuration
Write-Host "3. Getting current service configuration..." -ForegroundColor Yellow
aws apprunner describe-service `
    --service-arn $API_ARN `
    --region $AWS_REGION `
    --query "Service.SourceConfiguration" `
    > source-config.json

Write-Host "   ✅ Configuration retrieved" -ForegroundColor Green
Write-Host ""

# Check current CORS configuration
Write-Host "4. Current CORS configuration:" -ForegroundColor Yellow
$currentCors = aws apprunner describe-service `
    --service-arn $API_ARN `
    --region $AWS_REGION `
    --query "Service.SourceConfiguration.ImageRepository.ImageConfiguration.RuntimeEnvironmentVariables" `
    | ConvertFrom-Json

$corsKeys = $currentCors.PSObject.Properties | Where-Object { $_.Name -like "Cors__AllowedOrigins__*" }
if ($corsKeys.Count -gt 0) {
    foreach ($key in $corsKeys) {
        Write-Host "   $($key.Name): $($key.Value)" -ForegroundColor Gray
    }
} else {
    Write-Host "   ⚠️  No CORS configuration found!" -ForegroundColor Yellow
}
Write-Host ""

# Add CloudFront URL to configuration
Write-Host "5. Adding CloudFront URL to CORS..." -ForegroundColor Yellow
$config = Get-Content source-config.json | ConvertFrom-Json
if (-not $config.ImageRepository.ImageConfiguration.RuntimeEnvironmentVariables) {
    $config.ImageRepository.ImageConfiguration.RuntimeEnvironmentVariables = @{}
}
$config.ImageRepository.ImageConfiguration.RuntimeEnvironmentVariables."Cors__AllowedOrigins__10" = $CF_URL

$config | ConvertTo-Json -Depth 10 | Set-Content updated-source-config.json

Write-Host "   ✅ Configuration updated" -ForegroundColor Green
Write-Host ""

# Update the service
Write-Host "6. Updating App Runner service..." -ForegroundColor Yellow
Write-Host "   This will trigger a redeploy (~3-5 minutes)" -ForegroundColor Gray
Write-Host ""

aws apprunner update-service `
    --service-arn $API_ARN `
    --region $AWS_REGION `
    --source-configuration file://updated-source-config.json `
    --no-cli-pager

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ API Gateway CORS updated successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Next steps:" -ForegroundColor Cyan
    Write-Host "1. Wait ~3-5 minutes for deployment to complete" -ForegroundColor White
    Write-Host "2. Check service status:" -ForegroundColor White
    Write-Host "   aws apprunner describe-service --service-arn $API_ARN --region $AWS_REGION --query 'Service.Status'" -ForegroundColor Gray
    Write-Host "3. Test API Gateway:" -ForegroundColor White
    Write-Host "   curl https://ntcfgtkjwh.us-east-1.awsapprunner.com/health" -ForegroundColor Gray
    Write-Host "4. Test frontend (hard refresh):" -ForegroundColor White
    Write-Host "   $CF_URL" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🔍 Monitor deployment:" -ForegroundColor Cyan
    Write-Host "   Watch AWS Console: https://us-east-1.console.aws.amazon.com/apprunner/home?region=us-east-1#/services" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "❌ Failed to update service" -ForegroundColor Red
    exit 1
}

# Clean up temp files
Remove-Item source-config.json, updated-source-config.json -ErrorAction SilentlyContinue
