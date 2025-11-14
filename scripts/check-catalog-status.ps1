# Check Catalog Data Status
# Quick script to verify if catalog data exists in the database

$ErrorActionPreference = "Stop"

Write-Host "=== Checking Catalog Data Status ===" -ForegroundColor Cyan
Write-Host ""

# Get connection string from appsettings
$appSettingsPath = "backend/DataService/appsettings.Development.json"
if (-not (Test-Path $appSettingsPath)) {
    Write-Host "❌ appsettings.Development.json not found" -ForegroundColor Red
    exit 1
}

$appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
$connString = $appSettings.ConnectionStrings.DefaultConnection

if ([string]::IsNullOrEmpty($connString)) {
    Write-Host "❌ No connection string found" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Connection string found" -ForegroundColor Green

# Parse connection string
if ($connString -match "Host=([^;]+);.*Database=([^;]+);.*Username=([^;]+);.*Password=([^;]+)") {
    $dbHost = $matches[1]
    $dbName = $matches[2]
    $dbUser = $matches[3]
    $dbPassword = $matches[4]

    Write-Host "📊 Database: $dbName@$dbHost" -ForegroundColor Cyan
    Write-Host ""

    # Check if psql is available
    $psql = Get-Command psql -ErrorAction SilentlyContinue
    if ($psql) {
        Write-Host "Querying database..." -ForegroundColor Yellow

        # Set password environment variable
        $env:PGPASSWORD = $dbPassword

        # Query the count
        $query = "SELECT COUNT(*) FROM catalog_user.vessels_real;"
        $count = & psql -h $dbHost -U $dbUser -d $dbName -t -c $query 2>&1

        # Clear password
        $env:PGPASSWORD = $null

        if ($LASTEXITCODE -eq 0) {
            $count = $count.Trim()
            Write-Host ""
            if ($count -eq "0") {
                Write-Host "⚠️  Catalog is EMPTY (0 vessels)" -ForegroundColor Red
                Write-Host ""
                Write-Host "SOLUTION:" -ForegroundColor Yellow
                Write-Host "  Run: .\scripts\seed-catalog.ps1" -ForegroundColor White
            } else {
                Write-Host "✅ Catalog has $count vessels" -ForegroundColor Green
                Write-Host ""
                Write-Host "If the frontend shows 0 vessels, check:" -ForegroundColor Yellow
                Write-Host "  1. Backend API is running (port 5001 or 8080)" -ForegroundColor Gray
                Write-Host "  2. Frontend API URL is configured correctly" -ForegroundColor Gray
                Write-Host "  3. Check browser console for API errors" -ForegroundColor Gray
            }
        } else {
            Write-Host "❌ Database query failed. Schema may not exist yet." -ForegroundColor Red
            Write-Host ""
            Write-Host "SOLUTION:" -ForegroundColor Yellow
            Write-Host "  1. Apply migrations: cd backend/DataService && dotnet ef database update" -ForegroundColor White
            Write-Host "  2. Seed catalog: .\scripts\seed-catalog.ps1" -ForegroundColor White
        }
    } else {
        Write-Host "⚠️  psql not found. Cannot query database directly." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "To check status, either:" -ForegroundColor Yellow
        Write-Host "  1. Install PostgreSQL client tools" -ForegroundColor Gray
        Write-Host "  2. Check DataService startup logs for seeding messages" -ForegroundColor Gray
        Write-Host "  3. Use a database GUI (pgAdmin, DBeaver, etc.)" -ForegroundColor Gray
    }
} else {
    Write-Host "❌ Could not parse connection string" -ForegroundColor Red
}

Write-Host ""









