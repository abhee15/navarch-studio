#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Export Swagger/OpenAPI specifications from running backend services

.DESCRIPTION
    Downloads swagger.json from each running service and saves it to the service directory.
    These files are committed to git and used by CI/CD for type generation.

.EXAMPLE
    pwsh scripts/export-swagger.ps1

.NOTES
    Prerequisites:
    - Backend services must be running locally
    - Services must have Swagger UI enabled
#>

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Colors for output
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }
function Write-Error { Write-Host $args -ForegroundColor Red }

Write-Info "📤 Swagger/OpenAPI Export Tool"
Write-Host ""

# Get project root
$projectRoot = Split-Path -Parent $PSScriptRoot

# Define services
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

# Function to check if service is running
function Test-ServiceRunning {
    param([int]$Port)
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$Port/health" -TimeoutSec 2 -ErrorAction SilentlyContinue
        return $true
    } catch {
        return $false
    }
}

# Function to export swagger from a service
function Export-ServiceSwagger {
    param([hashtable]$Service)
    
    Write-Info "🔄 Exporting from $($Service.Name)..."
    
    # Check if service is running
    if (-not (Test-ServiceRunning -Port $Service.Port)) {
        Write-Error "   ❌ Service not running on port $($Service.Port)"
        Write-Warning "   Please start the service first"
        return $false
    }
    
    Write-Success "   ✓ Service is running"
    
    try {
        # Download swagger JSON
        Write-Info "   📥 Downloading swagger.json..."
        $swagger = Invoke-RestMethod -Uri $Service.SwaggerUrl -TimeoutSec 10
        
        # Convert to pretty JSON
        $prettyJson = $swagger | ConvertTo-Json -Depth 100
        
        # Add header comment
        $header = @"
{
  "x-generator": "Swagger Export Script",
  "x-generated": "$(Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")",
  "x-service": "$($Service.Name)",
  "x-note": "This file is auto-generated and committed to git for CI/CD type generation"
}
"@
        
        # Parse both JSONs and merge
        $headerObj = $header | ConvertFrom-Json
        $swaggerObj = $prettyJson | ConvertFrom-Json
        
        # Add header properties to swagger object
        $headerObj.PSObject.Properties | ForEach-Object {
            $swaggerObj | Add-Member -MemberType NoteProperty -Name $_.Name -Value $_.Value -Force
        }
        
        # Convert back to JSON
        $finalJson = $swaggerObj | ConvertTo-Json -Depth 100
        
        # Save to file
        $finalJson | Out-File -FilePath $Service.OutputFile -Encoding UTF8
        
        $relativePath = $Service.OutputFile -replace [regex]::Escape($projectRoot), '.'
        Write-Success "   ✅ Exported to $relativePath"
        
        # File size
        $fileSize = (Get-Item $Service.OutputFile).Length / 1KB
        Write-Info "   📦 Size: $([math]::Round($fileSize, 2)) KB"
        
        return $true
    } catch {
        Write-Error "   ❌ Export failed: $($_.Exception.Message)"
        return $false
    }
}

# Export from each service
Write-Host ""
$successCount = 0
$failCount = 0

foreach ($service in $services) {
    $success = Export-ServiceSwagger -Service $service
    if ($success) { $successCount++ } else { $failCount++ }
    Write-Host ""
}

# Summary
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
    Write-Info "📝 Exported files:"
    $services | ForEach-Object {
        if (Test-Path $_.OutputFile) {
            $relativePath = $_.OutputFile -replace [regex]::Escape($projectRoot), '.'
            Write-Host "   - $relativePath" -ForegroundColor Gray
        }
    }
    Write-Host ""
    Write-Info "Next steps:"
    Write-Host "   1. Review the exported files" -ForegroundColor Gray
    Write-Host "   2. Run: pwsh scripts/generate-types.ps1 -Mode ci" -ForegroundColor Gray
    Write-Host "   3. Commit the swagger.json files to git" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Error "❌ No files exported. Please start backend services and try again."
    Write-Host ""
    Write-Info "💡 To start services:"
    Write-Host "   cd backend/DataService && dotnet run" -ForegroundColor Gray
    Write-Host "   cd backend/HullSizingService && dotnet run" -ForegroundColor Gray
    Write-Host "   cd backend/IdentityService && dotnet run" -ForegroundColor Gray
    exit 1
}

