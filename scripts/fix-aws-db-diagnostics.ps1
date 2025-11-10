#!/usr/bin/env pwsh
# Fix AWS RDS database - add missing diagnostics_json column

$ErrorActionPreference = "Stop"

Write-Host "[FIX] Getting RDS credentials from AWS Secrets Manager..." -ForegroundColor Cyan
$dbPassword = (aws secretsmanager get-secret-value --secret-id navarch-studio-dev-db-password --region us-east-1 --query SecretString --output text | ConvertFrom-Json).password
$dbHost = "navarch-studio-dev-postgres.c50j4j58m2zt.us-east-1.rds.amazonaws.com"
$dbName = "navarch_studio_dev"
$dbUser = "postgres"

Write-Host "[FIX] Connecting to RDS and checking migrations..." -ForegroundColor Cyan

# Create temporary SQL script
$sqlScript = @"
-- Check current migrations
SELECT migration_id FROM sizing."__EFMigrationsHistory" ORDER BY migration_id;

-- Add missing diagnostics_json column
ALTER TABLE sizing.sizing_runs ADD COLUMN IF NOT EXISTS diagnostics_json TEXT NULL;

-- Mark the migration as applied
INSERT INTO sizing."__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20251108000000_AddDiagnosticsToSizingRuns', '8.0.0')
ON CONFLICT (migration_id) DO NOTHING;

-- Verify
SELECT migration_id FROM sizing."__EFMigrationsHistory" ORDER BY migration_id;
SELECT column_name, data_type FROM information_schema.columns
WHERE table_schema = 'sizing' AND table_name = 'sizing_runs'
ORDER BY ordinal_position;
"@

$sqlScript | Out-File -FilePath "temp-fix-db.sql" -Encoding utf8

Write-Host "[FIX] Applying SQL fix via Docker..." -ForegroundColor Cyan

# Use docker with environment variable for password to avoid URL encoding issues
docker run --rm -e PGPASSWORD="$dbPassword" -v "${PWD}:/scripts" postgres:15-alpine `
  psql -h $dbHost -U $dbUser -d $dbName -f /scripts/temp-fix-db.sql

Remove-Item "temp-fix-db.sql" -Force

Write-Host "[FIX] Database fix completed!" -ForegroundColor Green
Write-Host "[FIX] Please restart the HullSizingService in AWS App Runner to apply changes." -ForegroundColor Yellow
