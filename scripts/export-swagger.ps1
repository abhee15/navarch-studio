#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Export Swagger/OpenAPI specifications from running backend services

.DESCRIPTION
    Downloads swagger.json from each running service and saves to service directory.
    These files are committed to git and used by CI/CD for type generation.
#>

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }
function Write-Error { Write-Host $args -ForegroundColor Red }

Write-Info "📤 Swagger/OpenAPI Export Tool"
Write-Host ""

$projectRoot = Split-Path -Parent $PSScriptRoot

$services = @(
    @{
        Name = "DataService"
        SwaggerUrl = "http://localhost:5000/swagger/v1/swagger.json"
        OutputFile = "$projectRoot/backend/DataService/swagger.json"
        Port = 5000
    },
    @{
        Name = "HullSizingService"
        SwaggerUrl = "http://localhost:5003/swagger/v1/swagger.json"
        OutputFile = "$projectRoot/backend/HullSizingService/swagger.json"
        Port = 5003
    },
    @{
        Name = "IdentityService"
        SwaggerUrl = "http://localhost:5001/swagger/v1/swagger.json"
        OutputFile = "$projectRoot/backend/IdentityService/swagger.json"
        Port = 5001
    }
)

function Test-ServiceRunning {
    param([int]$Port)
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$Port/health" -TimeoutSec 2 -ErrorAction SilentlyContinue
        return $true
    } catch {
        return $false
    }
}

function Export-ServiceSwagger {
    param([hashtable]$Service)
    
    Write-Info "🔄 Exporting from $($Service.Name)..."
    
    if (-not (Test-ServiceRunning -Port $Service.Port)) {
        Write-Error "   ❌ Service not running on port $($Service.Port)"
        Write-Warning "   Please start the service first"
        return $false
    }
    
    Write-Success "   ✓ Service is running"
    
    try {
        Write-Info "   📥 Downloading swagger.json..."
        $swagger = Invoke-RestMethod -Uri $Service.SwaggerUrl -TimeoutSec 10
        
        $prettyJson = $swagger | ConvertTo-Json -Depth 100
        
        $prettyJson | Out-File -FilePath $Service.OutputFile -Encoding UTF8
        
        $relativePath = $Service.OutputFile -replace [regex]::Escape($projectRoot), '.'
        Write-Success "   ✅ Exported to $relativePath"
        
        $fileSize = (Get-Item $Service.OutputFile).Length / 1KB
        Write-Info "   📦 Size: $([math]::Round($fileSize, 2)) KB"
        
        return $true
    } catch {
        Write-Error "   ❌ Export failed: $($_.Exception.Message)"
        return $false
    }
}

Write-Host ""
$successCount = 0
$failCount = 0

foreach ($service in $services) {
    $success = Export-ServiceSwagger -Service $service
    if ($success) { $successCount++ } else { $failCount++ }
    Write-Host ""
}

Write-Info "=" * 70
Write-Info "Export Summary"
Write-Info "=" * 70
Write-Info "Succeeded: $successCount / $($services.Count)"
if ($failCount -gt 0) {
    Write-Warning "Failed: $failCount / $($services.Count)"
}
Write-Info "=" * 70
Write-Host ""

if ($successCount -gt 0) {
    Write-Success "🎉 Swagger export complete!"
    Write-Host ""
    Write-Info "Next steps:"
    Write-Host "   1. Run: pwsh scripts/generate-types.ps1 -Mode ci" -ForegroundColor Gray
    Write-Host "   2. Commit the swagger.json files to git" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Error "❌ No files exported. Please start backend services and try again."
    exit 1
}
