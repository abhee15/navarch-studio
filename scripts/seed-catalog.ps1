# Seed the vessel catalog data
# This script runs the DataService briefly to trigger catalog seeding

$ErrorActionPreference = "Stop"

Write-Host "=== Vessel Catalog Seeding Script ===" -ForegroundColor Cyan
Write-Host ""

# Check if DataService directory exists
if (-not (Test-Path "backend/DataService")) {
    Write-Host "❌ DataService directory not found. Run this from the project root." -ForegroundColor Red
    exit 1
}

# Check if CSV exists
$csvPath = "backend/DataService/Data/Seeds/vessel_catalog_curated_600.csv"
if (-not (Test-Path $csvPath)) {
    Write-Host "❌ Catalog CSV not found at: $csvPath" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Found catalog CSV with $(((Get-Content $csvPath | Measure-Object -Line).Lines - 1)) vessels" -ForegroundColor Green

# Check if database is configured
$connectionString = $env:ConnectionStrings__DefaultConnection
if ([string]::IsNullOrEmpty($connectionString)) {
    Write-Host "⚠️  No connection string found in environment. Checking appsettings..." -ForegroundColor Yellow

    # Try to read from appsettings.Development.json
    $appSettingsPath = "backend/DataService/appsettings.Development.json"
    if (Test-Path $appSettingsPath) {
        $appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
        $connectionString = $appSettings.ConnectionStrings.DefaultConnection
        if ([string]::IsNullOrEmpty($connectionString)) {
            Write-Host "❌ No connection string found in appsettings.Development.json" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "❌ appsettings.Development.json not found" -ForegroundColor Red
        exit 1
    }
}

Write-Host "✅ Database connection configured" -ForegroundColor Green
Write-Host ""

# Option 1: Run DataService temporarily
Write-Host "Running DataService to trigger seeding..." -ForegroundColor Cyan
Write-Host "This will:"
Write-Host "  1. Apply database migrations" -ForegroundColor Gray
Write-Host "  2. Seed catalog data (600+ vessels)" -ForegroundColor Gray
Write-Host "  3. The service will start and stay running" -ForegroundColor Gray
Write-Host ""
Write-Host "Press Ctrl+C to stop the service after you see 'Real-world vessel catalog seeding completed'" -ForegroundColor Yellow
Write-Host ""

Set-Location backend/DataService

try {
    # Run the DataService
    dotnet run --no-build
} catch {
    Write-Host "⚠️  Service stopped or interrupted" -ForegroundColor Yellow
}

Set-Location ../..

Write-Host ""
Write-Host "=== Seeding Complete ===" -ForegroundColor Cyan
Write-Host "Check the DataService logs above for:" -ForegroundColor Gray
Write-Host "  - '[SEED] Real-world vessel catalog seeding completed'" -ForegroundColor Gray
Write-Host "  - '[SEED] Imported X vessels'" -ForegroundColor Gray
Write-Host ""
Write-Host "You can now start your application normally." -ForegroundColor Green
