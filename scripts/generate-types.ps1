#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate TypeScript types from backend Swagger/OpenAPI specifications

.DESCRIPTION
    Uses NSwag to automatically generate TypeScript interfaces from C# DTOs.
    Works in both local development and GitHub Actions CI/CD.

.PARAMETER Mode
    'local' - Use running backend services (default)
    'ci' - Use pre-exported swagger.json files (for CI/CD)

.EXAMPLE
    pwsh scripts/generate-types.ps1 -Mode local
    pwsh scripts/generate-types.ps1 -Mode ci
#>

param(
    [Parameter()]
    [ValidateSet('local', 'ci')]
    [string]$Mode = 'local'
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }
function Write-Error { Write-Host $args -ForegroundColor Red }

Write-Info "🔧 TypeScript DTO Generation Tool"
Write-Info "Mode: $Mode"
Write-Host ""

# Check if NSwag is installed
Write-Info "Checking NSwag installation..."
try {
    $nswagVersion = nswag version 2>&1 | Select-Object -First 1
    Write-Success "✅ NSwag installed: $nswagVersion"
} catch {
    Write-Warning "⚠️  NSwag not found. Installing globally..."
    dotnet tool install -g NSwag.ConsoleCore --no-cache
    Write-Success "✅ NSwag installed successfully"
}

Write-Host ""

$projectRoot = Split-Path -Parent $PSScriptRoot

$services = @(
    @{
        Name = "DataService"
        DisplayName = "DataService (Hydrostatics)"
        SwaggerUrl = "http://localhost:5000/swagger/v1/swagger.json"
        SwaggerFile = "$projectRoot/backend/DataService/swagger.json"
        Output = "$projectRoot/frontend/src/api/generated/hydrostatics.types.ts"
        Port = 5000
    },
    @{
        Name = "HullSizingService"
        DisplayName = "HullSizingService"
        SwaggerUrl = "http://localhost:5003/swagger/v1/swagger.json"
        SwaggerFile = "$projectRoot/backend/HullSizingService/swagger.json"
        Output = "$projectRoot/frontend/src/api/generated/sizing.types.ts"
        Port = 5003
    },
    @{
        Name = "IdentityService"
        DisplayName = "IdentityService"
        SwaggerUrl = "http://localhost:5001/swagger/v1/swagger.json"
        SwaggerFile = "$projectRoot/backend/IdentityService/swagger.json"
        Output = "$projectRoot/frontend/src/api/generated/identity.types.ts"
        Port = 5001
    }
)

$outputDir = "$projectRoot/frontend/src/api/generated"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    Write-Success "✅ Created output directory: $outputDir"
}

function Test-ServiceRunning {
    param([int]$Port)
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$Port/health" -TimeoutSec 2 -ErrorAction SilentlyContinue
        return $true
    } catch {
        return $false
    }
}

function Generate-ServiceTypes {
    param(
        [hashtable]$Service,
        [string]$Mode
    )
    
    Write-Info "🔄 Generating types from $($Service.DisplayName)..."
    
    $swaggerSource = $null
    $useFile = $false
    
    if ($Mode -eq 'ci') {
        if (-not (Test-Path $Service.SwaggerFile)) {
            Write-Error "   ❌ Swagger file not found: $($Service.SwaggerFile)"
            Write-Warning "   Please run export-swagger.ps1 first"
            return $false
        }
        Write-Info "   📄 Using swagger.json file (CI mode)"
        $swaggerSource = $Service.SwaggerFile
        $useFile = $true
    } else {
        if (Test-ServiceRunning -Port $Service.Port) {
            Write-Success "   ✓ Service running on port $($Service.Port)"
            $swaggerSource = $Service.SwaggerUrl
        } elseif (Test-Path $Service.SwaggerFile) {
            Write-Warning "   ⚠ Service not running, using cached swagger.json"
            $swaggerSource = $Service.SwaggerFile
            $useFile = $true
        } else {
            Write-Error "   ❌ Service not running and no cached swagger.json found"
            Write-Warning "   Start the service or run: pwsh scripts/export-swagger.ps1"
            return $false
        }
    }
    
    $config = @{
        runtime = "Net80"
        documentGenerator = @{
            fromDocument = @{
                url = if ($useFile) { $swaggerSource } else { $swaggerSource }
            }
        }
        codeGenerators = @{
            openApiToTypeScriptClient = @{
                typeScriptVersion = 5.3
                template = "Fetch"
                generateClientClasses = $false
                generateClientInterfaces = $false
                generateDtoTypes = $true
                exportTypes = $true
                typeStyle = "Interface"
                enumStyle = "Enum"
                dateTimeType = "string"
                nullValue = "Undefined"
                markOptionalProperties = $true
                generateOptionalParameters = $true
                generateDefaultValues = $true
                convertConstructorInterfaceData = $false
                output = $Service.Output
            }
        }
    } | ConvertTo-Json -Depth 10
    
    $tempConfig = "$projectRoot/temp-nswag-$($Service.Name).json"
    $config | Out-File -FilePath $tempConfig -Encoding UTF8
    
    try {
        $nswagOutput = nswag run $tempConfig 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "   ✅ Generated $($Service.Output -replace [regex]::Escape($projectRoot), '.')"
            
            $generatedContent = Get-Content $Service.Output -Raw
            $header = @"
/**
 * AUTO-GENERATED TypeScript types from $($Service.DisplayName)
 * 
 * ⚠️  DO NOT EDIT THIS FILE MANUALLY
 * 
 * Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
 * Source: $($swaggerSource -replace [regex]::Escape($projectRoot), '.')
 * Mode: $Mode
 * 
 * To regenerate:
 *   npm run generate:types        (local development)
 *   npm run generate:types:ci     (CI/CD)
 */

$generatedContent
"@
            $header | Out-File -FilePath $Service.Output -Encoding UTF8
            
            return $true
        } else {
            Write-Error "   ❌ NSwag failed:"
            $nswagOutput | ForEach-Object { Write-Host "      $_" -ForegroundColor Red }
            return $false
        }
    } finally {
        if (Test-Path $tempConfig) {
            Remove-Item $tempConfig -Force
        }
    }
}

Write-Host ""
$successCount = 0
$failCount = 0
$results = @()

foreach ($service in $services) {
    $success = Generate-ServiceTypes -Service $service -Mode $Mode
    $results += @{
        Service = $service.DisplayName
        Success = $success
    }
    if ($success) { $successCount++ } else { $failCount++ }
    Write-Host ""
}

if ($successCount -gt 0) {
    Write-Info "📦 Creating index file..."
    $indexContent = @"
/**
 * AUTO-GENERATED type exports
 * 
 * Usage:
 *   import { VesselDto, LoadcaseDto } from '@/api/generated';
 * 
 * Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
 */

export * from './hydrostatics.types';
export * from './sizing.types';
export * from './identity.types';
"@

    $indexContent | Out-File -FilePath "$outputDir/index.ts" -Encoding UTF8
    Write-Success "✅ Created index.ts"
}

Write-Host ""
Write-Info "=" * 70
Write-Info "Type Generation Summary ($Mode mode)"
Write-Info "=" * 70

foreach ($result in $results) {
    $icon = if ($result.Success) { "✅" } else { "❌" }
    $color = if ($result.Success) { "Green" } else { "Red" }
    Write-Host "  $icon $($result.Service)" -ForegroundColor $color
}

Write-Host ""
Write-Info "Total: $successCount succeeded, $failCount failed"
Write-Info "=" * 70
Write-Host ""

if ($successCount -gt 0) {
    Write-Success "🎉 Type generation complete!"
    Write-Host ""
    Write-Info "💡 Usage in frontend:"
    Write-Host "   import { VesselDto, LoadcaseDto } from '@/api/generated';" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Error "❌ No types generated. Please check errors above."
    exit 1
}
