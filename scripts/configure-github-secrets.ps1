# Configure GitHub Secrets from Terraform Outputs
# This script sets up all required GitHub secrets for CI/CD workflows

$ErrorActionPreference = "Stop"

Write-Host "🔐 Configuring GitHub Secrets for CI/CD..." -ForegroundColor Yellow

# Check if GitHub CLI is installed
try {
    gh --version | Out-Null
}
catch {
    Write-Error "GitHub CLI (gh) is not installed. Install from: https://cli.github.com/"
    exit 1
}

# Check if authenticated with GitHub
try {
    gh auth status | Out-Null
}
catch {
    Write-Host "Please authenticate with GitHub CLI:" -ForegroundColor Yellow
    gh auth login
}

# Get Terraform outputs
Write-Host "📥 Fetching Terraform outputs..." -ForegroundColor Cyan
Push-Location terraform/setup

try {
    $outputs = terraform output -json | ConvertFrom-Json

    # Extract values
    $s3BucketName = $outputs.s3_bucket_name.value
    $dynamodbTableName = $outputs.dynamodb_table_name.value
    $vpcId = $outputs.vpc_id.value
    $publicSubnetIds = $outputs.public_subnet_ids.value
    $appRunnerSgId = $outputs.app_runner_security_group_id.value
    $rdsSgId = $outputs.rds_security_group_id.value
    $ecrUrls = $outputs.ecr_repository_urls.value
    $cognitoUserPoolId = $outputs.cognito_user_pool_id.value
    $cognitoUserPoolClientId = $outputs.cognito_user_pool_client_id.value

    Write-Host "✅ Terraform outputs fetched successfully" -ForegroundColor Green
}
catch {
    Write-Error "Failed to get Terraform outputs: $($_.Exception.Message)"
    exit 1
}
finally {
    Pop-Location
}

# Set GitHub Secrets
Write-Host "`n🔑 Setting GitHub Secrets..." -ForegroundColor Cyan

# AWS Credentials (need to be set manually - we'll remind the user)
Write-Host "`n⚠️  AWS Credentials must be set manually:" -ForegroundColor Yellow
Write-Host "   Run these commands with your AWS access key:" -ForegroundColor Yellow
Write-Host "   gh secret set AWS_ACCESS_KEY_ID --body `"YOUR_ACCESS_KEY`"" -ForegroundColor White
Write-Host "   gh secret set AWS_SECRET_ACCESS_KEY --body `"YOUR_SECRET_KEY`"" -ForegroundColor White

# Project Configuration
Write-Host "`nSetting project configuration..." -ForegroundColor Cyan
gh secret set PROJECT_NAME --body "sri-test-project-1"
gh secret set COST_CENTER --body "engineering"
gh secret set DOMAIN_NAME --body ""

# Infrastructure IDs
Write-Host "Setting infrastructure IDs..." -ForegroundColor Cyan
gh secret set S3_BUCKET_NAME --body $s3BucketName
gh secret set DYNAMODB_TABLE_NAME --body $dynamodbTableName
gh secret set VPC_ID --body $vpcId
gh secret set PUBLIC_SUBNET_IDS --body ($publicSubnetIds | ConvertTo-Json -Compress)
gh secret set APP_RUNNER_SECURITY_GROUP_ID --body $appRunnerSgId
gh secret set RDS_SECURITY_GROUP_ID --body $rdsSgId

# ECR Repository URLs
Write-Host "Setting ECR repository URLs..." -ForegroundColor Cyan
gh secret set ECR_IDENTITY_SERVICE_URL --body $ecrUrls.identity_service
gh secret set ECR_API_GATEWAY_URL --body $ecrUrls.api_gateway
gh secret set ECR_DATA_SERVICE_URL --body $ecrUrls.data_service
gh secret set ECR_FRONTEND_URL --body $ecrUrls.frontend

# Cognito Configuration
Write-Host "Setting Cognito configuration..." -ForegroundColor Cyan
gh secret set COGNITO_USER_POOL_ID --body $cognitoUserPoolId
gh secret set COGNITO_USER_POOL_CLIENT_ID --body $cognitoUserPoolClientId

# Database Configuration (user needs to set password)
Write-Host "`nSetting database configuration..." -ForegroundColor Cyan
gh secret set RDS_DATABASE --body "sri_template_db"
gh secret set RDS_USERNAME --body "postgres"

Write-Host "`n⚠️  Database password must be set manually:" -ForegroundColor Yellow
Write-Host "   gh secret set RDS_PASSWORD --body `"YOUR_SECURE_PASSWORD`"" -ForegroundColor White

Write-Host "`n✅ GitHub secrets configured successfully!" -ForegroundColor Green

Write-Host "`n📋 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Set AWS credentials (see above)" -ForegroundColor White
Write-Host "2. Set RDS password (see above)" -ForegroundColor White
Write-Host "3. Re-enable workflows in .github/workflows/ci-dev.yml and ci-staging.yml" -ForegroundColor White
Write-Host "4. Push a commit to test the CI/CD pipeline" -ForegroundColor White

Write-Host "`n🎉 You're ready to deploy!" -ForegroundColor Green






