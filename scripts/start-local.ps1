#!/usr/bin/env pwsh
# Start Local Development Environment
# This script starts all services using docker-compose

param(
    [switch]$Build,
    [switch]$Clean
)

Write-Host "🚀 Starting NavArch Studio Local Environment" -ForegroundColor Cyan
Write-Host "=" * 60

# Check if Docker is running
try {
    docker info > $null 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Docker is not running"
    }
} catch {
    Write-Host "❌ Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Clean up if requested
if ($Clean) {
    Write-Host "`n🧹 Cleaning up existing containers and volumes..." -ForegroundColor Yellow
    docker-compose down -v
    Write-Host "✅ Cleanup complete" -ForegroundColor Green
}

# Build if requested or if images don't exist
if ($Build) {
    Write-Host "`n🔨 Building Docker images..." -ForegroundColor Yellow
    docker-compose build --parallel
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build complete" -ForegroundColor Green
}

# Start services
Write-Host "`n🚀 Starting services..." -ForegroundColor Yellow
Write-Host ""
Write-Host "This will start:" -ForegroundColor White
Write-Host "  • PostgreSQL (port 5433)" -ForegroundColor Gray
Write-Host "  • pgAdmin (port 5050)" -ForegroundColor Gray
Write-Host "  • Identity Service (port 5001)" -ForegroundColor Gray
Write-Host "  • Data Service (port 5003)" -ForegroundColor Gray
Write-Host "  • API Gateway (port 5002)" -ForegroundColor Gray
Write-Host "  • Frontend (port 3000)" -ForegroundColor Gray
Write-Host ""

docker-compose up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to start services" -ForegroundColor Red
    exit 1
}

# Wait a moment for services to start
Write-Host "`n⏳ Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Check service status
Write-Host "`n📊 Service Status:" -ForegroundColor Cyan
docker-compose ps

# Show logs hint
Write-Host "`n" + ("=" * 60) -ForegroundColor Cyan
Write-Host "✅ Local environment started!" -ForegroundColor Green
Write-Host ("=" * 60) -ForegroundColor Cyan

Write-Host "`n🌐 Access your services:" -ForegroundColor Cyan
Write-Host "  Frontend:        http://localhost:3000" -ForegroundColor White
Write-Host "  API Gateway:     http://localhost:5002" -ForegroundColor White
Write-Host "  Identity Service: http://localhost:5001" -ForegroundColor White
Write-Host "  Data Service:    http://localhost:5003" -ForegroundColor White
Write-Host "  pgAdmin:         http://localhost:5050" -ForegroundColor White
Write-Host "                   (admin@example.com / admin)" -ForegroundColor Gray

Write-Host "`n📋 Useful commands:" -ForegroundColor Cyan
Write-Host "  View logs:       docker-compose logs -f" -ForegroundColor White
Write-Host "  View logs (svc): docker-compose logs -f [service-name]" -ForegroundColor White
Write-Host "  Stop services:   docker-compose down" -ForegroundColor White
Write-Host "  Restart:         docker-compose restart" -ForegroundColor White
Write-Host "  Stop & clean:    docker-compose down -v" -ForegroundColor White

Write-Host "`n💡 Tips:" -ForegroundColor Cyan
Write-Host "  • Services may take 30-60 seconds to fully start" -ForegroundColor White
Write-Host "  • Database migrations run automatically on startup" -ForegroundColor White
Write-Host "  • Use local auth mode (no Cognito required)" -ForegroundColor White
Write-Host "  • Frontend uses http://localhost:5002 for API calls" -ForegroundColor White

Write-Host "`n📺 Watch logs:" -ForegroundColor Yellow
Write-Host "  docker-compose logs -f" -ForegroundColor White
Write-Host ""

