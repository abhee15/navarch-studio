# PowerShell script to reset AWS RDS database schema
# This will drop and recreate the 'data' schema to fix migration issues

$ErrorActionPreference = "Stop"
$ENVIRONMENT = "dev"
$AWS_REGION = "us-east-1"

Write-Host "🔄 Resetting database schema for environment: $ENVIRONMENT" -ForegroundColor Cyan
Write-Host ""

# Step 1: Get RDS endpoint
Write-Host "📦 Step 1: Getting RDS endpoint..." -ForegroundColor Yellow
$RDS_INSTANCES = aws rds describe-db-instances `
    --region $AWS_REGION `
    --query "DBInstances[?contains(DBInstanceIdentifier, 'navarch-studio-$ENVIRONMENT')].{ID:DBInstanceIdentifier,Endpoint:Endpoint.Address}" `
    --output json | ConvertFrom-Json

if ($RDS_INSTANCES.Count -eq 0) {
    Write-Host "❌ No RDS instance found for environment: $ENVIRONMENT" -ForegroundColor Red
    Write-Host "Please check AWS Console: https://console.aws.amazon.com/rds/home?region=$AWS_REGION#databases:" -ForegroundColor Yellow
    exit 1
}

$RDS_ENDPOINT = $RDS_INSTANCES[0].Endpoint
$RDS_ID = $RDS_INSTANCES[0].ID

Write-Host "✅ Found RDS instance: $RDS_ID" -ForegroundColor Green
Write-Host "   Endpoint: $RDS_ENDPOINT" -ForegroundColor Gray
Write-Host ""

# Step 2: Get database credentials
Write-Host "🔐 Step 2: Getting database credentials from Secrets Manager..." -ForegroundColor Yellow
$SECRET_NAME = "navarch-studio/$ENVIRONMENT/rds-credentials"

try {
    $SECRET_JSON = aws secretsmanager get-secret-value `
        --secret-id $SECRET_NAME `
        --region $AWS_REGION `
        --query SecretString `
        --output text | ConvertFrom-Json

    $DB_USERNAME = $SECRET_JSON.username
    $DB_PASSWORD = $SECRET_JSON.password
    $DB_NAME = "navarch_data"

    Write-Host "✅ Credentials retrieved" -ForegroundColor Green
    Write-Host "   Username: $DB_USERNAME" -ForegroundColor Gray
    Write-Host "   Database: $DB_NAME" -ForegroundColor Gray
} catch {
    Write-Host "❌ Failed to retrieve credentials from Secrets Manager" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 3: Create SQL commands
Write-Host "📝 Step 3: Preparing SQL commands..." -ForegroundColor Yellow
$SQL_COMMANDS = @"
-- Drop existing 'data' schema and all its tables
DROP SCHEMA IF EXISTS data CASCADE;

-- Create fresh 'data' schema
CREATE SCHEMA data;

-- Grant permissions to database user
GRANT ALL ON SCHEMA data TO $DB_USERNAME;

-- Delete migration history to force re-migration
DELETE FROM public."__EFMigrationsHistory";

-- Show what we have now
SELECT schema_name FROM information_schema.schemata WHERE schema_name = 'data';
"@

Write-Host "✅ SQL commands prepared" -ForegroundColor Green
Write-Host ""

# Step 4: Connect and execute
Write-Host "🔌 Step 4: Connecting to database and executing SQL..." -ForegroundColor Yellow
Write-Host "   This will:" -ForegroundColor Gray
Write-Host "   1. Drop 'data' schema (and all tables in it)" -ForegroundColor Gray
Write-Host "   2. Create fresh 'data' schema" -ForegroundColor Gray
Write-Host "   3. Delete migration history" -ForegroundColor Gray
Write-Host ""

# Set password environment variable
$env:PGPASSWORD = $DB_PASSWORD

# Check if psql is installed
$PSQL_PATH = Get-Command psql -ErrorAction SilentlyContinue

if (-not $PSQL_PATH) {
    Write-Host "❌ psql is not installed or not in PATH" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install PostgreSQL client:" -ForegroundColor Yellow
    Write-Host "  - Download from: https://www.postgresql.org/download/windows/" -ForegroundColor Cyan
    Write-Host "  - Or use WSL: sudo apt-get install postgresql-client" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "📋 Manual SQL commands to run:" -ForegroundColor Yellow
    Write-Host $SQL_COMMANDS -ForegroundColor Gray
    Write-Host ""
    Write-Host "Connection details:" -ForegroundColor Yellow
    Write-Host "  Host: $RDS_ENDPOINT" -ForegroundColor Gray
    Write-Host "  User: $DB_USERNAME" -ForegroundColor Gray
    Write-Host "  Database: $DB_NAME" -ForegroundColor Gray
    Write-Host "  Password: <in Secrets Manager: $SECRET_NAME>" -ForegroundColor Gray
    exit 1
}

Write-Host "Executing SQL commands..." -ForegroundColor Yellow
try {
    $SQL_COMMANDS | psql -h $RDS_ENDPOINT -U $DB_USERNAME -d $DB_NAME -v ON_ERROR_STOP=1
    Write-Host "✅ SQL commands executed successfully" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to execute SQL commands" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
} finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD
}

Write-Host ""

# Step 5: Trigger Data Service restart
Write-Host "🚀 Step 5: Triggering Data Service restart..." -ForegroundColor Yellow

$DATA_SERVICE_ARN = aws apprunner list-services `
    --region $AWS_REGION `
    --query "ServiceSummaryList[?contains(ServiceName, '$ENVIRONMENT-data-service')].ServiceArn | [0]" `
    --output text

if ([string]::IsNullOrEmpty($DATA_SERVICE_ARN) -or $DATA_SERVICE_ARN -eq "None") {
    Write-Host "⚠️  Could not find Data Service ARN" -ForegroundColor Yellow
    Write-Host "Please manually restart the Data Service in AWS Console" -ForegroundColor Yellow
} else {
    Write-Host "Found Data Service ARN: $DATA_SERVICE_ARN" -ForegroundColor Gray
    aws apprunner start-deployment --service-arn $DATA_SERVICE_ARN --region $AWS_REGION

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Data Service deployment triggered" -ForegroundColor Green
        Write-Host "   Migrations will run automatically on startup" -ForegroundColor Gray
        Write-Host "   Wait ~3-5 minutes for deployment to complete" -ForegroundColor Gray
    } else {
        Write-Host "⚠️  Failed to trigger deployment" -ForegroundColor Yellow
        Write-Host "Please manually restart the Data Service in AWS Console" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "✅ Database schema reset complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Next steps:" -ForegroundColor Cyan
Write-Host "1. Wait for Data Service deployment to complete (~3-5 minutes)" -ForegroundColor White
Write-Host "2. Check CloudWatch logs:" -ForegroundColor White
Write-Host "   aws logs tail /aws/apprunner/navarch-studio-$ENVIRONMENT-data-service --since 5m --region $AWS_REGION --filter-pattern `"[MIGRATION]`" --follow" -ForegroundColor Gray
Write-Host "3. Verify tables were created:" -ForegroundColor White
Write-Host "   psql -h $RDS_ENDPOINT -U $DB_USERNAME -d $DB_NAME -c `"\dt data.*`"" -ForegroundColor Gray
Write-Host "4. Test the vessels endpoint from the frontend" -ForegroundColor White
Write-Host ""
