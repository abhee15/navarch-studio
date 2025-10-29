# Service Health Check Script
# Tests all deployed services independently to diagnose issues

param(
    [string]$Environment = "dev"
)

$ErrorActionPreference = "Continue"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Service Health Check - $Environment" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Service URLs (update these based on deployment)
$services = @{
    "Identity Service" = "https://zgs9ncdp4i.us-east-1.awsapprunner.com"
    "Data Service"     = "https://mhbkzb73ev.us-east-1.awsapprunner.com"
    "API Gateway"      = "https://ie2ijkwmns.us-east-1.awsapprunner.com"
}

$results = @()

# Test each service
foreach ($serviceName in $services.Keys) {
    $baseUrl = $services[$serviceName]
    Write-Host "`n--- Testing: $serviceName ---" -ForegroundColor Yellow
    Write-Host "Base URL: $baseUrl"
    
    # Test /health endpoint
    Write-Host "Testing /health..."
    try {
        $response = Invoke-WebRequest -Uri "$baseUrl/health" -Method Get -TimeoutSec 10
        Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Green
        Write-Host "  Content: $($response.Content)"
        $results += [PSCustomObject]@{
            Service  = $serviceName
            Endpoint = "/health"
            Status   = $response.StatusCode
            Success  = $true
        }
    }
    catch {
        Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
        $results += [PSCustomObject]@{
            Service  = $serviceName
            Endpoint = "/health"
            Status   = if ($_.Exception.Response) { $_.Exception.Response.StatusCode.value__ } else { "N/A" }
            Success  = $false
        }
    }
}

# Test Data Service directly
Write-Host "`n--- Testing Data Service Vessels Endpoint (Direct) ---" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://mhbkzb73ev.us-east-1.awsapprunner.com/api/v1/hydrostatics/vessels" -Method Get -TimeoutSec 10
    Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "  Content: $($response.Content)"
}
catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "  Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Yellow
    }
}

# Test Data Service through API Gateway
Write-Host "`n--- Testing Data Service Vessels Endpoint (via API Gateway) ---" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://ie2ijkwmns.us-east-1.awsapprunner.com/api/v1/hydrostatics/vessels" -Method Get -TimeoutSec 10
    Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "  Content: $($response.Content)"
}
catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "  Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Yellow
    }
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Summary" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$results | Format-Table -AutoSize

$failedServices = $results | Where-Object { -not $_.Success }
if ($failedServices.Count -gt 0) {
    Write-Host "`nFailed Services:" -ForegroundColor Red
    $failedServices | Format-Table -AutoSize
    exit 1
}
else {
    Write-Host "`nAll services are healthy!" -ForegroundColor Green
    exit 0
}

