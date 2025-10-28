# Quick Deploy Fix to AWS
# This script builds the fixed Docker images and deploys them to AWS

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment = "dev"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Quick Deploy Fix to AWS" -ForegroundColor Cyan
Write-Host "  Environment: $Environment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Get project root
$ProjectRoot = Split-Path -Parent $PSScriptRoot

# Step 1: Build backend locally to verify
Write-Host "`n[1/5] Building backend locally to verify fixes..." -ForegroundColor Yellow
Push-Location "$ProjectRoot/backend"
dotnet clean | Out-Null
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Backend build failed" -ForegroundColor Red
    Pop-Location
    exit 1
}
Write-Host "✅ Backend build successful" -ForegroundColor Green

# Verify config file is in output
$configPath = "DataService/bin/Debug/net8.0/config/unit-systems.xml"
if (Test-Path $configPath) {
    Write-Host "✅ Config file found in DataService output" -ForegroundColor Green
}
else {
    Write-Host "❌ Config file NOT found in DataService output" -ForegroundColor Red
    Pop-Location
    exit 1
}

Pop-Location

# Step 2: Build and push Docker images
Write-Host "`n[2/5] Building and pushing Docker images to ECR..." -ForegroundColor Yellow
& "$ProjectRoot/scripts/build-and-push.ps1" -Environment $Environment
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Docker build and push failed" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Docker images built and pushed" -ForegroundColor Green

# Step 3: Wait for user confirmation before deploying
Write-Host "`n[3/5] Docker images ready. Proceed with deployment?" -ForegroundColor Yellow
Write-Host "This will update the App Runner services in AWS." -ForegroundColor Yellow
$confirmation = Read-Host "Continue? (y/n)"
if ($confirmation -ne 'y') {
    Write-Host "Deployment cancelled" -ForegroundColor Yellow
    exit 0
}

# Step 4: Deploy infrastructure (this will pull the new images)
Write-Host "`n[4/5] Deploying to AWS App Runner..." -ForegroundColor Yellow
Write-Host "Note: App Runner will automatically pull the new images with :latest tag" -ForegroundColor Cyan
& "$ProjectRoot/scripts/deploy.ps1" -Environment $Environment -AutoApprove
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Deployment failed" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Deployment successful" -ForegroundColor Green

# Step 5: Instructions for verification
Write-Host "`n[5/5] Deployment Complete!" -ForegroundColor Green
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Verification Steps" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n1. Check CloudWatch Logs:" -ForegroundColor Yellow
Write-Host "   - Navigate to AWS Console > CloudWatch > Log Groups" -ForegroundColor White
Write-Host "   - Look for log groups starting with /aws/apprunner/" -ForegroundColor White
Write-Host "   - Verify you see: 'Unit conversion service registered with default config path'" -ForegroundColor White

Write-Host "`n2. Test the Application:" -ForegroundColor Yellow
Write-Host "   - Open your application frontend" -ForegroundColor White
Write-Host "   - Try creating a new vessel" -ForegroundColor White
Write-Host "   - Verify no 500 errors occur" -ForegroundColor White
Write-Host "   - Check that settings load correctly" -ForegroundColor White

Write-Host "`n3. Monitor App Runner Services:" -ForegroundColor Yellow
Write-Host "   - AWS Console > App Runner" -ForegroundColor White
Write-Host "   - Verify all services are 'Running' and 'Healthy'" -ForegroundColor White
Write-Host "   - Check that new deployments completed successfully" -ForegroundColor White

Write-Host "`n4. If Issues Occur:" -ForegroundColor Yellow
Write-Host "   - Check CloudWatch Logs for detailed error messages" -ForegroundColor White
Write-Host "   - Verify RDS database is accessible" -ForegroundColor White
Write-Host "   - Ensure environment variables are correct in App Runner" -ForegroundColor White
Write-Host "   - Review AWS_UNIT_CONVERSION_FIX.md for troubleshooting" -ForegroundColor White

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Deployment Summary:" -ForegroundColor Yellow
Write-Host "- Backend rebuilt with config file fixes" -ForegroundColor White
Write-Host "- Docker images built and pushed to ECR" -ForegroundColor White
Write-Host "- App Runner services updated with new images" -ForegroundColor White
Write-Host "- Unit conversion service should now work correctly" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan

