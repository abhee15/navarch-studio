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
    # Local development (backend must be running)
    pwsh scripts/generate-types.ps1 -Mode local

    # CI/CD (uses committed swagger.json files)
    pwsh scripts/generate-types.ps1 -Mode ci

.NOTES
    Prerequisites:
    - NSwag.ConsoleCore (installed automatically if missing)
    - For local: Backend services running
    - For CI: Swagger JSON files exported to backend/{service}/swagger.json
#>

param(
    [Parameter()]
    [ValidateSet('local', 'ci')]
    [string]$Mode = 'local'
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Colors for output
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

# Get project root (scripts directory is in root)
$projectRoot = Split-Path -Parent $PSScriptRoot

# Define services to generate types from
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

# Create output directory if it doesn't exist
$outputDir = "$projectRoot/frontend/src/api/generated"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    Write-Success "✅ Created output directory: $outputDir"
}

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

# Function to generate types for a service
function Generate-ServiceTypes {
    param(
        [hashtable]$Service,
        [string]$Mode
    )
    
    Write-Info "🔄 Generating types from $($Service.DisplayName)..."
    
    # Determine Swagger source based on mode
    $swaggerSource = $null
    $useFile = $false
    
    if ($Mode -eq 'ci') {
        # CI mode: Always use swagger.json files
        if (-not (Test-Path $Service.SwaggerFile)) {
            Write-Error "   ❌ Swagger file not found: $($Service.SwaggerFile)"
            Write-Warning "   Please run export-swagger.ps1 first to generate swagger.json files"
            return $false
        }
        Write-Info "   📄 Using swagger.json file (CI mode)"
        $swaggerSource = $Service.SwaggerFile
        $useFile = $true
    } else {
        # Local mode: Prefer running service, fallback to file
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
    
    # Create NSwag configuration
    $config = @{
        runtime = "Net80"
        documentGenerator = @{
            fromDocument = @{
                url = if ($useFile) { $swaggerSource } else { $swaggerSource }
            }
        }
        codeGenerators = @{
            openApiToTypeScriptClient = @{
                # TypeScript settings
                typeScriptVersion = 5.3
                template = "Fetch"
                
                # Don't generate API clients, only types
                generateClientClasses = $false
                generateClientInterfaces = $false
                
                # Generate DTOs
                generateDtoTypes = $true
                exportTypes = $true
                typeStyle = "Interface"
                enumStyle = "Enum"
                
                # Type mapping
                dateTimeType = "string"  # Dates as ISO strings
                nullValue = "Undefined"  # Use undefined for nulls
                markOptionalProperties = $true
                generateOptionalParameters = $true
                generateDefaultValues = $true
                
                # Naming
                convertConstructorInterfaceData = $false
                
                # Output
                output = $Service.Output
            }
        }
    } | ConvertTo-Json -Depth 10
    
    # Write temporary config file
    $tempConfig = "$projectRoot/temp-nswag-$($Service.Name).json"
    $config | Out-File -FilePath $tempConfig -Encoding UTF8
    
    try {
        # Run NSwag
        $nswagOutput = nswag run $tempConfig 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "   ✅ Generated $($Service.Output -replace [regex]::Escape($projectRoot), '.')"
            
            # Add header comment to generated file
            $generatedContent = Get-Content $Service.Output -Raw
            $header = @"
/**
 * AUTO-GENERATED TypeScript types from $($Service.DisplayName)
 * 
 * ⚠️  DO NOT EDIT THIS FILE MANUALLY
 * 
 * Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
 * Source: $($swaggerSource -replace [regex]::Escape($projectRoot), '.')
 * Generator: NSwag v$nswagVersion
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
        # Clean up temp config
        if (Test-Path $tempConfig) {
            Remove-Item $tempConfig -Force
        }
    }
}

# Generate types for each service
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

# Create index file to re-export all types
if ($successCount -gt 0) {
    Write-Info "📦 Creating index file..."
    $indexContent = @"
/**
 * AUTO-GENERATED type exports
 * 
 * This file re-exports all generated types for convenient importing.
 * 
 * Usage:
 *   import { VesselDto, LoadcaseDto } from '@/api/generated';
 * 
 * Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
 */

// Hydrostatics types (DataService)
export * from './hydrostatics.types';

// Hull Sizing types
export * from './sizing.types';

// Identity types
export * from './identity.types';
"@

    $indexContent | Out-File -FilePath "$outputDir/index.ts" -Encoding UTF8
    Write-Success "✅ Created index.ts"
}

# Create README if it doesn't exist
$readmePath = "$outputDir/README.md"
if (-not (Test-Path $readmePath)) {
    Write-Info "📝 Creating README..."
    $readmeContent = @"
# Generated TypeScript Types

⚠️ **DO NOT EDIT FILES IN THIS DIRECTORY MANUALLY**

These TypeScript interfaces are automatically generated from the backend C# DTOs using NSwag.

## Regenerating Types

### Local Development
\`\`\`bash
# From project root
pwsh scripts/generate-types.ps1

# Or from frontend directory
npm run generate:types
\`\`\`

### CI/CD
\`\`\`bash
# Uses committed swagger.json files
pwsh scripts/generate-types.ps1 -Mode ci

# Or
npm run generate:types:ci
\`\`\`

## How It Works

1. **Export Stage**: Backend services export Swagger/OpenAPI spec to \`swagger.json\`
2. **Generate Stage**: NSwag reads the spec and generates TypeScript interfaces
3. **Commit Stage**: Generated types are committed to git (ensures consistency)
4. **Use Stage**: Frontend code imports and uses the types

## Benefits

- ✅ **No manual sync** - Types stay in sync with backend automatically
- ✅ **Type safety** - Compiler catches API contract changes
- ✅ **Autocomplete** - Better IDE support
- ✅ **Documentation** - XML comments from C# DTOs included
- ✅ **CI/CD ready** - Works in GitHub Actions without running services

## File Organization

\`\`\`
frontend/src/api/generated/
├── hydrostatics.types.ts  # From DataService
├── sizing.types.ts        # From HullSizingService  
├── identity.types.ts      # From IdentityService
├── index.ts               # Re-exports all types
└── README.md              # This file
\`\`\`

## Troubleshooting

### "Service not running" error (Local mode)

Start the backend service:
\`\`\`bash
cd backend/DataService
dotnet run
\`\`\`

Or use CI mode with cached files:
\`\`\`bash
pwsh scripts/generate-types.ps1 -Mode ci
\`\`\`

### "Swagger file not found" error (CI mode)

Export swagger files first:
\`\`\`bash
pwsh scripts/export-swagger.ps1
\`\`\`

### Generated types look wrong

Check NSwag configuration in \`scripts/generate-types.ps1\`

### Types are out of sync

1. Make sure backend changes are committed
2. Run \`pwsh scripts/export-swagger.ps1\` if DTOs changed
3. Run \`pwsh scripts/generate-types.ps1\`
4. Commit the generated files

## Workflow

### When Backend DTOs Change

1. Update C# DTOs in \`backend/Shared/DTOs/\`
2. Run backend service locally
3. Export swagger: \`pwsh scripts/export-swagger.ps1\`
4. Generate types: \`pwsh scripts/generate-types.ps1\`
5. Commit all changes (including generated files)
6. CI will validate types match

### In Pull Requests

GitHub Actions will:
- Regenerate types from committed swagger.json files
- Compare with committed types
- Fail PR if types are out of sync
- Ensure frontend always matches backend
"@

    $readmeContent | Out-File -FilePath $readmePath -Encoding UTF8
    Write-Success "✅ Created README.md"
}

# Summary
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
    Write-Info "📝 Generated files:"
    Get-ChildItem -Path "$outputDir/*.types.ts" | ForEach-Object {
        $relativePath = $_.FullName -replace [regex]::Escape($projectRoot), '.'
        Write-Host "   - $relativePath" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Info "💡 Usage in frontend:"
    Write-Host "   import { VesselDto, LoadcaseDto } from '@/api/generated';" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Error "❌ No types generated. Please check errors above."
    Write-Host ""
    Write-Info "💡 Troubleshooting:"
    if ($Mode -eq 'local') {
        Write-Host "   - Ensure backend services are running" -ForegroundColor Gray
        Write-Host "   - Or use CI mode: pwsh scripts/generate-types.ps1 -Mode ci" -ForegroundColor Gray
    } else {
        Write-Host "   - Run: pwsh scripts/export-swagger.ps1" -ForegroundColor Gray
        Write-Host "   - This exports swagger.json files from running services" -ForegroundColor Gray
    }
    exit 1
}
