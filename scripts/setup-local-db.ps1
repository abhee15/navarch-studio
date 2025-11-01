#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Setup local database for docker-compose development environment
.DESCRIPTION
    Applies Entity Framework migrations and seeds the database with test data
.EXAMPLE
    ./scripts/setup-local-db.ps1
#>

param(
    [switch]$SkipMigrations,
    [switch]$SkipSeeds
)

$ErrorActionPreference = "Stop"

Write-Host "🔄 Setting up local database..." -ForegroundColor Cyan

# Check if docker-compose is running
Write-Host "`nChecking if docker-compose services are running..." -ForegroundColor Yellow
$postgresRunning = docker compose ps postgres --quiet
if (-not $postgresRunning) {
    Write-Host "❌ PostgreSQL container is not running. Please run 'docker compose up -d postgres' first." -ForegroundColor Red
    exit 1
}
Write-Host "✅ PostgreSQL is running" -ForegroundColor Green

# Wait for PostgreSQL to be ready
Write-Host "`nWaiting for PostgreSQL to be ready..." -ForegroundColor Yellow
$maxRetries = 30
$retry = 0
while ($retry -lt $maxRetries) {
    $result = docker compose exec -T postgres pg_isready -U postgres 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ PostgreSQL is ready" -ForegroundColor Green
        break
    }
    $retry++
    Write-Host "⏳ Waiting for PostgreSQL... ($retry/$maxRetries)" -ForegroundColor Yellow
    Start-Sleep -Seconds 1
}

if ($retry -eq $maxRetries) {
    Write-Host "❌ PostgreSQL did not become ready in time" -ForegroundColor Red
    exit 1
}

# Apply migrations
if (-not $SkipMigrations) {
    Write-Host "`n🔄 Applying Entity Framework migrations..." -ForegroundColor Cyan

    Write-Host "`n  📦 IdentityService migrations..." -ForegroundColor Yellow
    Push-Location backend/IdentityService
    try {
        dotnet ef database update --connection "Host=localhost;Port=5433;Database=sri_template_dev;Username=postgres;Password=postgres"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Failed to apply IdentityService migrations" -ForegroundColor Red
            exit 1
        }
        Write-Host "  ✅ IdentityService migrations applied" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }

    Write-Host "`n  📦 DataService migrations..." -ForegroundColor Yellow
    Push-Location backend/DataService
    try {
        dotnet ef database update --connection "Host=localhost;Port=5433;Database=sri_template_dev;Username=postgres;Password=postgres"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Failed to apply DataService migrations" -ForegroundColor Red
            exit 1
        }
        Write-Host "  ✅ DataService migrations applied" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host "`n⏭️  Skipping migrations (--SkipMigrations flag set)" -ForegroundColor Yellow
}

# Seed data
if (-not $SkipSeeds) {
    Write-Host "`n🌱 Seeding database with test data..." -ForegroundColor Cyan

    $seedFile = "database/seeds/dev-seed.sql"
    if (Test-Path $seedFile) {
        Get-Content $seedFile | docker compose exec -T postgres psql -U postgres -d sri_template_dev
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Failed to seed database" -ForegroundColor Red
            exit 1
        }
        Write-Host "✅ Database seeded successfully" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️  Seed file not found: $seedFile" -ForegroundColor Yellow
    }
}
else {
    Write-Host "`n⏭️  Skipping seeds (--SkipSeeds flag set)" -ForegroundColor Yellow
}

Write-Host "`n✅ Local database setup complete!" -ForegroundColor Green
Write-Host "`n📝 Test users created:" -ForegroundColor Cyan
Write-Host "   • admin@example.com (password: password)" -ForegroundColor White
Write-Host "   • user@example.com (password: password)" -ForegroundColor White
Write-Host "`n🚀 You can now login to the application" -ForegroundColor Green
