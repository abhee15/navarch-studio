#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Comprehensive diagnostics for hydrostatics vessel creation timeout
.DESCRIPTION
    Tests each layer of the stack to identify where the 30-second timeout occurs
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$Environment = "dev"
)

$ErrorActionPreference = "Continue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Timeout Diagnostics - $Environment Environment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Get service URLs from AWS
Write-Host "Step 1: Getting service URLs from AWS..." -ForegroundColor Yellow
try {
    $services = aws apprunner list-services --region us-east-1 --output json | ConvertFrom-Json
    
    $identityService = $services.ServiceSummaryList | Where-Object { $_.ServiceName -like "*$Environment-identity-service" }
    $apiGateway = $services.ServiceSummaryList | Where-Object { $_.ServiceName -like "*$Environment-api-gateway" }
    $dataService = $services.ServiceSummaryList | Where-Object { $_.ServiceName -like "*$Environment-data-service" }
    
    $identityUrl = "https://$($identityService.ServiceUrl)"
    $apiGatewayUrl = "https://$($apiGateway.ServiceUrl)"
    $dataServiceUrl = "https://$($dataService.ServiceUrl)"
    
    Write-Host "✓ Identity Service: $identityUrl" -ForegroundColor Green
    Write-Host "✓ API Gateway: $apiGatewayUrl" -ForegroundColor Green
    Write-Host "✓ Data Service: $dataServiceUrl" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "✗ Failed to get service URLs from AWS" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# Step 2: Check service health endpoints
Write-Host "Step 2: Checking service health endpoints..." -ForegroundColor Yellow

function Test-HealthEndpoint {
    param($Name, $Url)
    
    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-WebRequest -Uri "$Url/health" -Method GET -TimeoutSec 10
        $stopwatch.Stop()
        
        Write-Host "  ✓ $Name health: $($response.StatusCode) (${stopwatch.ElapsedMilliseconds}ms)" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "  ✗ $Name health: FAILED - $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

$identityHealthy = Test-HealthEndpoint "Identity Service" $identityUrl
$apiHealthy = Test-HealthEndpoint "API Gateway" $apiGatewayUrl
$dataHealthy = Test-HealthEndpoint "Data Service" $dataServiceUrl
Write-Host ""

if (-not ($identityHealthy -and $apiHealthy -and $dataHealthy)) {
    Write-Host "⚠ One or more services are unhealthy. Check service status in AWS Console." -ForegroundColor Yellow
    Write-Host ""
}

# Step 3: Check API Gateway network configuration
Write-Host "Step 3: Checking API Gateway network configuration..." -ForegroundColor Yellow
try {
    $apiGatewayArn = $apiGateway.ServiceArn
    $apiConfig = aws apprunner describe-service --service-arn $apiGatewayArn --region us-east-1 --output json | ConvertFrom-Json
    
    $egressType = $apiConfig.Service.NetworkConfiguration.EgressConfiguration.EgressType
    Write-Host "  Egress Type: $egressType" -ForegroundColor Cyan
    
    if ($egressType -eq "VPC") {
        $vpcConnectorArn = $apiConfig.Service.NetworkConfiguration.EgressConfiguration.VpcConnectorArn
        Write-Host "  VPC Connector: $vpcConnectorArn" -ForegroundColor Cyan
        Write-Host "  ⚠ VPC egress requires NAT Gateway for internet access" -ForegroundColor Yellow
    }
    elseif ($egressType -eq "DEFAULT") {
        Write-Host "  ✓ Using DEFAULT egress (public internet)" -ForegroundColor Green
    }
    Write-Host ""
}
catch {
    Write-Host "  ✗ Failed to get API Gateway configuration" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
}

# Step 4: Check API Gateway environment variables
Write-Host "Step 4: Checking API Gateway service URLs configuration..." -ForegroundColor Yellow
try {
    $envVars = $apiConfig.Service.SourceConfiguration.ImageRepository.ImageConfiguration.RuntimeEnvironmentVariables
    
    $configuredDataService = $envVars.Services__DataService
    $configuredIdentityService = $envVars.Services__IdentityService
    
    Write-Host "  Configured DataService URL: $configuredDataService" -ForegroundColor Cyan
    Write-Host "  Actual DataService URL: $dataServiceUrl" -ForegroundColor Cyan
    
    if ($configuredDataService -ne $dataServiceUrl) {
        Write-Host "  ✗ MISMATCH! API Gateway is configured with wrong DataService URL" -ForegroundColor Red
    }
    else {
        Write-Host "  ✓ DataService URL matches" -ForegroundColor Green
    }
    Write-Host ""
}
catch {
    Write-Host "  ✗ Failed to check environment variables" -ForegroundColor Red
    Write-Host ""
}

# Step 5: Test API Gateway → DataService connectivity
Write-Host "Step 5: Testing API Gateway can reach DataService..." -ForegroundColor Yellow
Write-Host "  This test makes a direct call to DataService health endpoint" -ForegroundColor Gray

if ($dataHealthy) {
    Write-Host "  ✓ DataService is healthy and publicly accessible" -ForegroundColor Green
    Write-Host "  ⚠ If API Gateway times out but DataService is healthy, check:" -ForegroundColor Yellow
    Write-Host "    - API Gateway egress configuration" -ForegroundColor Yellow
    Write-Host "    - Service-to-service URL configuration" -ForegroundColor Yellow
    Write-Host "    - CloudWatch logs for connection errors" -ForegroundColor Yellow
}
else {
    Write-Host "  ✗ DataService is not accessible" -ForegroundColor Red
}
Write-Host ""

# Step 6: Check recent CloudWatch logs
Write-Host "Step 6: Checking recent CloudWatch logs..." -ForegroundColor Yellow

function Get-RecentLogs {
    param($ServiceName, $LogGroup)
    
    Write-Host "  Fetching logs from $ServiceName..." -ForegroundColor Gray
    try {
        $logs = aws logs tail $LogGroup --since 10m --format short --region us-east-1 2>&1
        
        # Look for specific error patterns
        $timeoutErrors = $logs | Select-String -Pattern "timeout|timed out" -CaseSensitive:$false
        $connectionErrors = $logs | Select-String -Pattern "connection refused|cannot connect|no such host" -CaseSensitive:$false
        $jwtErrors = $logs | Select-String -Pattern "jwt|cognito|unauthorized" -CaseSensitive:$false
        $dbErrors = $logs | Select-String -Pattern "database|postgres|migration" -CaseSensitive:$false
        
        if ($timeoutErrors) {
            Write-Host "    ⚠ Found timeout errors:" -ForegroundColor Yellow
            $timeoutErrors | ForEach-Object { Write-Host "      $_" -ForegroundColor Gray }
        }
        
        if ($connectionErrors) {
            Write-Host "    ⚠ Found connection errors:" -ForegroundColor Yellow
            $connectionErrors | ForEach-Object { Write-Host "      $_" -ForegroundColor Gray }
        }
        
        if ($jwtErrors) {
            Write-Host "    ⚠ Found JWT/auth errors:" -ForegroundColor Yellow
            $jwtErrors | ForEach-Object { Write-Host "      $_" -ForegroundColor Gray }
        }
        
        if ($dbErrors) {
            Write-Host "    ℹ Database-related logs:" -ForegroundColor Cyan
            $dbErrors | ForEach-Object { Write-Host "      $_" -ForegroundColor Gray }
        }
        
        if (-not ($timeoutErrors -or $connectionErrors -or $jwtErrors -or $dbErrors)) {
            Write-Host "    ✓ No obvious errors in recent logs" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "    ✗ Failed to fetch logs: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
}

$apiLogGroup = "/aws/apprunner/navarch-studio-$Environment-api-gateway"
$dataLogGroup = "/aws/apprunner/navarch-studio-$Environment-data-service"

Get-RecentLogs "API Gateway" $apiLogGroup
Get-RecentLogs "Data Service" $dataLogGroup

# Step 7: Check RDS connectivity
Write-Host "Step 7: Checking RDS configuration..." -ForegroundColor Yellow
try {
    $rdsInstances = aws rds describe-db-instances --region us-east-1 --output json | ConvertFrom-Json
    $rdsInstance = $rdsInstances.DBInstances | Where-Object { $_.DBInstanceIdentifier -like "*$Environment*" } | Select-Object -First 1
    
    if ($rdsInstance) {
        Write-Host "  Database: $($rdsInstance.DBInstanceIdentifier)" -ForegroundColor Cyan
        Write-Host "  Status: $($rdsInstance.DBInstanceStatus)" -ForegroundColor Cyan
        Write-Host "  Endpoint: $($rdsInstance.Endpoint.Address)" -ForegroundColor Cyan
        
        if ($rdsInstance.DBInstanceStatus -ne "available") {
            Write-Host "  ✗ Database is not available!" -ForegroundColor Red
        }
        else {
            Write-Host "  ✓ Database is available" -ForegroundColor Green
        }
    }
    else {
        Write-Host "  ⚠ No RDS instance found for $Environment environment" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "  ✗ Failed to check RDS status" -ForegroundColor Red
}
Write-Host ""

# Step 8: Summary and recommendations
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Summary and Recommendations" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Common Causes of 30-Second Timeouts:" -ForegroundColor Yellow
Write-Host "1. API Gateway egress = VPC without NAT Gateway" -ForegroundColor Gray
Write-Host "   → App Runner with VPC egress needs NAT Gateway to reach internet/other services" -ForegroundColor Gray
Write-Host "   → Solution: Change to DEFAULT egress OR add NAT Gateway" -ForegroundColor Gray
Write-Host ""
Write-Host "2. API Gateway configured with wrong service URLs" -ForegroundColor Gray
Write-Host "   → Check Services__DataService environment variable" -ForegroundColor Gray
Write-Host "   → Must match actual DataService App Runner URL" -ForegroundColor Gray
Write-Host ""
Write-Host "3. DataService taking too long to respond" -ForegroundColor Gray
Write-Host "   → Check DataService CloudWatch logs for slow queries" -ForegroundColor Gray
Write-Host "   → Database connection issues" -ForegroundColor Gray
Write-Host "   → Pending migrations blocking operations" -ForegroundColor Gray
Write-Host ""
Write-Host "4. JWT authentication middleware hanging" -ForegroundColor Gray
Write-Host "   → CognitoJwtService can't reach Cognito JWKS endpoint" -ForegroundColor Gray
Write-Host "   → Check API Gateway can reach internet for Cognito" -ForegroundColor Gray
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Review the diagnostic output above for any red ✗ marks" -ForegroundColor White
Write-Host "2. Check CloudWatch logs in detail:" -ForegroundColor White
Write-Host "   aws logs tail $apiLogGroup --follow --region us-east-1" -ForegroundColor Gray
Write-Host "3. If API Gateway egress = VPC, consider changing to DEFAULT:" -ForegroundColor White
Write-Host "   - Update terraform/deploy/modules/app-runner/main.tf" -ForegroundColor Gray
Write-Host "   - Run terraform apply" -ForegroundColor Gray
Write-Host "4. Test vessel creation and monitor logs in real-time" -ForegroundColor White
Write-Host ""

