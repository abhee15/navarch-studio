#!/bin/bash

# Script to diagnose AWS RDS database issues
# Usage: ./scripts/diagnose-aws-database.sh <environment>

ENVIRONMENT=${1:-dev}
AWS_REGION="us-east-1"

echo "🔍 Diagnosing AWS RDS for environment: $ENVIRONMENT"
echo ""

# Get RDS endpoint from Terraform outputs
cd terraform/deploy
RDS_ENDPOINT=$(terraform output -raw rds_endpoint 2>/dev/null || echo "")

if [ -z "$RDS_ENDPOINT" ]; then
    echo "❌ Could not get RDS endpoint from Terraform"
    echo "Run: cd terraform/deploy && terraform output rds_endpoint"
    exit 1
fi

echo "📦 RDS Endpoint: $RDS_ENDPOINT"
echo ""

# Get database credentials from AWS Secrets Manager
SECRET_NAME="navarch-studio/${ENVIRONMENT}/rds-credentials"
echo "🔐 Fetching credentials from Secrets Manager: $SECRET_NAME"

SECRET_JSON=$(aws secretsmanager get-secret-value \
    --secret-id "$SECRET_NAME" \
    --region "$AWS_REGION" \
    --query SecretString \
    --output text 2>/dev/null)

if [ $? -ne 0 ]; then
    echo "❌ Failed to fetch database credentials"
    echo "Make sure you have AWS credentials configured"
    exit 1
fi

DB_USERNAME=$(echo "$SECRET_JSON" | jq -r '.username')
DB_PASSWORD=$(echo "$SECRET_JSON" | jq -r '.password')
DB_NAME="navarch_data"

echo "✅ Credentials retrieved"
echo ""

# Create psql connection string
export PGPASSWORD="$DB_PASSWORD"

echo "=================================="
echo "1. Checking Database Connection"
echo "=================================="
psql -h "$RDS_ENDPOINT" -U "$DB_USERNAME" -d "$DB_NAME" -c "SELECT version();" 2>&1 | head -3

if [ $? -ne 0 ]; then
    echo "❌ Cannot connect to database"
    exit 1
fi

echo ""
echo "=================================="
echo "2. Checking Schemas"
echo "=================================="
psql -h "$RDS_ENDPOINT" -U "$DB_USERNAME" -d "$DB_NAME" -c "
SELECT schema_name 
FROM information_schema.schemata 
WHERE schema_name NOT IN ('pg_catalog', 'information_schema')
ORDER BY schema_name;"

echo ""
echo "=================================="
echo "3. Checking All Tables"
echo "=================================="
psql -h "$RDS_ENDPOINT" -U "$DB_USERNAME" -d "$DB_NAME" -c "
SELECT table_schema, table_name 
FROM information_schema.tables 
WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
ORDER BY table_schema, table_name;"

echo ""
echo "=================================="
echo "4. Checking 'vessels' Table"
echo "=================================="
psql -h "$RDS_ENDPOINT" -U "$DB_USERNAME" -d "$DB_NAME" -c "
SELECT table_schema, table_name, column_name, data_type, is_nullable
FROM information_schema.columns 
WHERE table_name = 'vessels'
ORDER BY table_schema, ordinal_position;"

echo ""
echo "=================================="
echo "5. Checking Migration History"
echo "=================================="
psql -h "$RDS_ENDPOINT" -U "$DB_USERNAME" -d "$DB_NAME" -c "
SELECT * FROM public.\"__EFMigrationsHistory\" ORDER BY \"MigrationId\";"

echo ""
echo "=================================="
echo "6. Checking Data in 'data.vessels'"
echo "=================================="
psql -h "$RDS_ENDPOINT" -U "$DB_USERNAME" -d "$DB_NAME" -c "
SELECT COUNT(*) as vessel_count FROM data.vessels;" 2>&1

echo ""
echo "=================================="
echo "7. Testing Query from VesselService"
echo "=================================="
psql -h "$RDS_ENDPOINT" -U "$DB_USERNAME" -d "$DB_NAME" -c "
SELECT id, user_id, name, lpp, beam, design_draft, created_at, updated_at, deleted_at
FROM data.vessels 
WHERE user_id = '00000000-0000-0000-0000-000000000001' AND deleted_at IS NULL
ORDER BY updated_at DESC;" 2>&1

echo ""
echo "✅ Diagnosis complete"
echo ""
echo "💡 If 'vessels' table doesn't exist, the migration didn't run"
echo "💡 If 'units_system' column exists, there's a schema mismatch"
echo "💡 Check CloudWatch logs for actual error: [VESSELS] ERROR"

# Clean up
unset PGPASSWORD

