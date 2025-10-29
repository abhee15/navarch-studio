#!/usr/bin/env pwsh
# Script to check App Runner service health and diagnose timeout issues

param(
    [string]$Environment = "dev"
)

Write-Host "🔍 Checking NavArch Studio Service Health ($Environment)" -ForegroundColor Cyan
Write-Host "=" * 60

# Get Terraform outputs
Write-Host "`n📋 Getting service URLs from Terraform..." -ForegroundColor Yellow
Push-Location terraform/deploy
try {
    $outputs = terraform output -json | ConvertFrom-Json
    $apiGatewayUrl = $outputs.api_gateway_url.value
    $identityServiceUrl = $outputs.identity_service_url.value
    $dataServiceUrl = $outputs.data_service_url.value
    $frontendUrl = $outputs.frontend_url.value
}
catch {
    Write-Host "❌ Failed to get Terraform outputs. Is infrastructure deployed?" -ForegroundColor Red
    Write-Host "   Run: cd terraform/deploy && terraform output" -ForegroundColor Yellow
    Pop-Location
    exit 1
}
Pop-Location

Write-Host "✅ Service URLs retrieved" -ForegroundColor Green

# Function to test endpoint
function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [int]$TimeoutSeconds = 30
    )

    Write-Host "`n🔎 Testing $Name..." -ForegroundColor Yellow
    Write-Host "   URL: $Url" -ForegroundColor Gray

    try {
        $response = Invoke-WebRequest -Uri "$Url/health" -Method Get -TimeoutSec $TimeoutSeconds -UseBasicParsing

        if ($response.StatusCode -eq 200) {
            $content = $response.Content | ConvertFrom-Json
            Write-Host "✅ $Name is healthy" -ForegroundColor Green
            Write-Host "   Status: $($content.status)" -ForegroundColor Gray
            if ($content.timestamp) {
                Write-Host "   Timestamp: $($content.timestamp)" -ForegroundColor Gray
            }
            return $true
        }
        else {
            Write-Host "⚠️  $Name returned HTTP $($response.StatusCode)" -ForegroundColor Yellow
            return $false
        }
    }
    catch {
        Write-Host "❌ $Name is NOT responding" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red

        if ($_.Exception.Message -like "*timeout*") {
            Write-Host "   ⏱️  Request timed out - service may be starting or unhealthy" -ForegroundColor Yellow
        }

        return $false
    }
}

# Test all services
$results = @{
    IdentityService = Test-Endpoint "Identity Service" $identityServiceUrl
    DataService     = Test-Endpoint "Data Service" $dataServiceUrl
    APIGateway      = Test-Endpoint "API Gateway" $apiGatewayUrl
    Frontend        = Test-Endpoint "Frontend (CloudFront)" $frontendUrl -TimeoutSeconds 10
}

# Test hydrostatics endpoint specifically
Write-Host "`n🔎 Testing Hydrostatics Endpoint..." -ForegroundColor Yellow
try {
    $hydroUrl = "$apiGatewayUrl/api/v1/hydrostatics/vessels"
    Write-Host "   URL: $hydroUrl" -ForegroundColor Gray

    # This will require auth, but we're just checking if it responds
    $response = Invoke-WebRequest -Uri $hydroUrl -Method Get -TimeoutSec 30 -UseBasicParsing -SkipHttpErrorCheck

    if ($response.StatusCode -in 200, 401, 403) {
        Write-Host "✅ Hydrostatics endpoint is responding (HTTP $($response.StatusCode))" -ForegroundColor Green
        if ($response.StatusCode -eq 401) {
            Write-Host "   (401 is expected without auth token)" -ForegroundColor Gray
        }
    }
    else {
        Write-Host "⚠️  Hydrostatics endpoint returned HTTP $($response.StatusCode)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Hydrostatics endpoint is NOT responding" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Summary
Write-Host "`n" + ("=" * 60) -ForegroundColor Cyan
Write-Host "📊 Health Check Summary" -ForegroundColor Cyan
Write-Host ("=" * 60) -ForegroundColor Cyan

$healthyCount = ($results.Values | Where-Object { $_ }).Count
$totalCount = $results.Count

Write-Host "`nServices Healthy: $healthyCount / $totalCount" -ForegroundColor $(if ($healthyCount -eq $totalCount) { "Green" } else { "Yellow" })

foreach ($service in $results.Keys) {
    $status = if ($results[$service]) { "✅ Healthy" } else { "❌ Unhealthy" }
    $color = if ($results[$service]) { "Green" } else { "Red" }
    Write-Host "  $service`: $status" -ForegroundColor $color
}

# Recommendations
if ($healthyCount -lt $totalCount) {
    Write-Host "`n💡 Recommendations:" -ForegroundColor Yellow
    Write-Host "   1. Check if services are deployed:" -ForegroundColor White
    Write-Host "      aws apprunner list-services --region us-east-1" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   2. Check service status:" -ForegroundColor White
    Write-Host "      aws apprunner describe-service --service-arn <service-arn> --region us-east-1" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   3. Check service logs in CloudWatch:" -ForegroundColor White
    Write-Host "      aws logs tail /aws/apprunner/navarch-studio-$Environment-data-service --follow" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   4. Trigger a new deployment:" -ForegroundColor White
    Write-Host "      - Push code to trigger CI/CD, or" -ForegroundColor Gray
    Write-Host "      - Manually trigger workflow in GitHub Actions" -ForegroundColor Gray
}
else {
    Write-Host "`n✅ All services are healthy!" -ForegroundColor Green
    Write-Host "   If you're still experiencing timeouts, check:" -ForegroundColor White
    Write-Host "   - Browser console for frontend errors" -ForegroundColor Gray
    Write-Host "   - Network tab for failed requests" -ForegroundColor Gray
    Write-Host "   - CloudWatch logs for backend errors" -ForegroundColor Gray
}

Write-Host ""
