# Check if API Gateway CORS is actually configured
$ErrorActionPreference = "Stop"

Write-Host "🔍 Checking API Gateway CORS Configuration..." -ForegroundColor Cyan
Write-Host ""

# Get API Gateway service ARN
$API_ARN = aws apprunner list-services --region us-east-1 `
    --query "ServiceSummaryList[?contains(ServiceName, 'dev-api-gateway')].ServiceArn | [0]" `
    --output text

if ([string]::IsNullOrEmpty($API_ARN) -or $API_ARN -eq "None") {
    Write-Host "❌ Could not find API Gateway service" -ForegroundColor Red
    exit 1
}

Write-Host "Service ARN: $API_ARN" -ForegroundColor Gray
Write-Host ""

# Get current environment variables
Write-Host "📋 Current Environment Variables:" -ForegroundColor Yellow
$envVars = aws apprunner describe-service --service-arn $API_ARN --region us-east-1 `
    --query "Service.SourceConfiguration.ImageRepository.ImageConfiguration.RuntimeEnvironmentVariables" `
    | ConvertFrom-Json

# Check for CORS configuration
Write-Host ""
Write-Host "CORS Configuration:" -ForegroundColor Cyan
$corsFound = $false
foreach ($prop in $envVars.PSObject.Properties) {
    if ($prop.Name -like "Cors__*") {
        Write-Host "  $($prop.Name): $($prop.Value)" -ForegroundColor Green
        $corsFound = $true
    }
}

if (-not $corsFound) {
    Write-Host "  ❌ NO CORS CONFIGURATION FOUND!" -ForegroundColor Red
    Write-Host ""
    Write-Host "This means the update-cors job either:" -ForegroundColor Yellow
    Write-Host "  1. Didn't run" -ForegroundColor Gray
    Write-Host "  2. Failed silently" -ForegroundColor Gray
    Write-Host "  3. Was overwritten by subsequent deployment" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Solution: Run the manual CORS fix script:" -ForegroundColor Cyan
    Write-Host "  powershell -ExecutionPolicy Bypass -File scripts/fix-cors-manually.ps1" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "✅ CORS configuration found!" -ForegroundColor Green
}

Write-Host ""
Write-Host "All Environment Variables:" -ForegroundColor Yellow
foreach ($prop in $envVars.PSObject.Properties) {
    Write-Host "  $($prop.Name): $($prop.Value)" -ForegroundColor Gray
}
