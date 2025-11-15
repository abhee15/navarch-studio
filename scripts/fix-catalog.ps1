# Fix Missing Catalog Data
# This script builds and runs DataService to seed the vessel catalog

$ErrorActionPreference = "Stop"

Write-Host "=== Fix Missing Catalog Data ===" -ForegroundColor Cyan
Write-Host ""

# Navigate to backend directory
Push-Location backend

try {
    Write-Host "📦 Step 1: Building DataService..." -ForegroundColor Yellow
    dotnet build DataService/DataService.csproj

    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed" -ForegroundColor Red
        exit 1
    }

    Write-Host "✅ Build successful" -ForegroundColor Green
    Write-Host ""

    Write-Host "🌱 Step 2: Running DataService to seed catalog..." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Watch for these messages in the output:" -ForegroundColor Cyan
    Write-Host "  - '[SEED] Checking for real-world vessel catalog...'" -ForegroundColor Gray
    Write-Host "  - '[SEED] Real-world vessel catalog seeding completed'" -ForegroundColor Gray
    Write-Host "  - '✅ Real-world catalog import successful'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "The service will start and run. Press Ctrl+C after you see the seeding complete." -ForegroundColor Yellow
    Write-Host ""

    Start-Sleep -Seconds 2

    # Run DataService
    Push-Location DataService
    dotnet run

} catch {
    Write-Host ""
    Write-Host "Service stopped" -ForegroundColor Yellow
} finally {
    Pop-Location
    Pop-Location
}

Write-Host ""
Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Check the output above for '[SEED] Real-world vessel catalog seeding completed'" -ForegroundColor White
Write-Host "2. If successful, refresh your browser to see the vessels" -ForegroundColor White
Write-Host "3. If it shows 'already seeded', the data is there - check API connectivity" -ForegroundColor White
Write-Host ""











