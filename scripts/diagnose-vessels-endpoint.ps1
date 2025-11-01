#!/usr/bin/env pwsh
# Diagnostic script for vessels endpoint 500 error

param(
    [string]$Region = "us-east-1",
    [string]$ProjectName = "navarch-studio"
)

Write-Host "`n=== Vessels Endpoint Diagnostic ===" -ForegroundColor Cyan

# 1. Check Data Service logs for errors
Write-Host "`n1. Checking Data Service logs for recent errors..." -ForegroundColor Yellow
aws logs tail "/aws/apprunner/$ProjectName-data-service" `
    --since 10m `
    --format short `
    --region $Region `
    --filter-pattern "ERROR" 2>&1 | Select-Object -Last 20

# 2. Check for migration/seeding logs
Write-Host "`n2. Checking for migration and seeding logs..." -ForegroundColor Yellow
aws logs tail "/aws/apprunner/$ProjectName-data-service" `
    --since 30m `
    --format short `
    --region $Region `
    --filter-pattern "MIGRATION" 2>&1 | Select-Object -Last 10

Write-Host "`n3. Checking for seed logs..." -ForegroundColor Yellow
aws logs tail "/aws/apprunner/$ProjectName-data-service" `
    --since 30m `
    --format short `
    --region $Region `
    --filter-pattern "SEED" 2>&1 | Select-Object -Last 10

# 4. Check for vessels endpoint specific logs
Write-Host "`n4. Checking vessels endpoint logs..." -ForegroundColor Yellow
aws logs tail "/aws/apprunner/$ProjectName-data-service" `
    --since 10m `
    --format short `
    --region $Region `
    --filter-pattern "VESSELS" 2>&1 | Select-Object -Last 20

# 5. Check API Gateway logs
Write-Host "`n5. Checking API Gateway logs..." -ForegroundColor Yellow
aws logs tail "/aws/apprunner/$ProjectName-api-gateway" `
    --since 10m `
    --format short `
    --region $Region `
    --filter-pattern "hydrostatics/vessels" 2>&1 | Select-Object -Last 10

# 6. Check deployment status
Write-Host "`n6. Checking App Runner service status..." -ForegroundColor Yellow
$services = aws apprunner list-services --region $Region --query "ServiceSummaryList[?contains(ServiceName, '$ProjectName')].{Name:ServiceName,Status:Status,Id:ServiceId}" --output json | ConvertFrom-Json

foreach ($service in $services) {
    Write-Host "  - $($service.Name): $($service.Status)" -ForegroundColor $(if ($service.Status -eq "RUNNING") { "Green" } else { "Red" })
}

Write-Host "`n=== Diagnostic Complete ===" -ForegroundColor Cyan
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Check for ERROR messages in Data Service logs above"
Write-Host "2. Verify template vessel was seeded (look for 'Template vessel seeding completed')"
Write-Host "3. Check if database migration completed successfully"
Write-Host "4. If template seeding failed, manually trigger: POST /api/v1/hydrostatics/vessels/seed-template"

