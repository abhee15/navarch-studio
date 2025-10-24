# Automated template sync with config protection
param(
    [string]$TemplateBranch = "main"
)

$ProtectedFiles = @(
    "backend/sri-subscription.sln",
    "docker-compose.yml",
    "docker-compose.override.yml",
    "frontend/vite.config.ts",
    "terraform/setup/terraform.tfvars",
    "terraform/deploy/environments/dev.tfvars",
    "terraform/deploy/environments/staging.tfvars",
    "terraform/deploy/environments/prod.tfvars",
    "README.md",
    "backend/IdentityService/appsettings.json",
    "backend/IdentityService/appsettings.Development.json",
    "backend/ApiGateway/appsettings.json",
    "backend/ApiGateway/appsettings.Development.json",
    "backend/DataService/appsettings.json",
    "backend/DataService/appsettings.Development.json",
    "backend/IdentityService/Properties/launchSettings.json",
    "backend/ApiGateway/Properties/launchSettings.json",
    "backend/DataService/Properties/launchSettings.json"
)

Write-Host "🔄 Syncing from sri-template..." -ForegroundColor Green

# 1. Backup protected files
Write-Host "📦 Backing up protected configs..." -ForegroundColor Yellow
$BackupDir = "$env:TEMP\sri-sub-backup-$(Get-Date -Format yyyyMMddHHmmss)"
New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null

foreach ($file in $ProtectedFiles) {
    if (Test-Path $file) {
        $destDir = Join-Path $BackupDir (Split-Path $file -Parent)
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        Copy-Item $file -Destination (Join-Path $BackupDir $file) -Force
        Write-Host "  ✓ Backed up: $file" -ForegroundColor Gray
    }
}

# 2. Fetch and merge template
Write-Host "🔀 Merging from template/$TemplateBranch..." -ForegroundColor Yellow
git fetch template
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to fetch template. Make sure the template remote is configured:" -ForegroundColor Red
    Write-Host "  git remote add template https://github.com/abhee15/sri-template.git" -ForegroundColor Cyan
    exit 1
}

git merge "template/$TemplateBranch" --no-commit --no-ff
if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 1) {
    Write-Host "⚠️  Merge conflicts detected. Restoring protected files..." -ForegroundColor Yellow
}

# 3. Restore protected files
Write-Host "♻️  Restoring protected configs..." -ForegroundColor Yellow
foreach ($file in $ProtectedFiles) {
    $backupPath = Join-Path $BackupDir $file
    if (Test-Path $backupPath) {
        Copy-Item $backupPath -Destination $file -Force
        git add $file
        Write-Host "  ✓ Restored: $file" -ForegroundColor Gray
    }
}

# 4. Show status
Write-Host "`n📊 Merge status:" -ForegroundColor Yellow
git status

Write-Host "`n✅ Template sync complete!" -ForegroundColor Green
Write-Host "Review changes and commit:" -ForegroundColor Yellow
Write-Host "  git commit -m 'Sync from sri-template (configs preserved)'" -ForegroundColor Cyan
Write-Host "  git push origin $(git branch --show-current)" -ForegroundColor Cyan

