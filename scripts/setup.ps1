# AWS Infrastructure Setup Script
param(
    [string]$ProjectName = "sri-subscription",
    [string]$AwsRegion = "us-east-1",
    [string]$AwsAccountId = "",
    [string]$BudgetEmail = "",
    [string]$CostCenter = "engineering"
)

Write-Host "🚀 Setting up AWS infrastructure for $ProjectName" -ForegroundColor Green

# Check prerequisites
Write-Host "`n📋 Checking prerequisites..." -ForegroundColor Yellow
& "$PSScriptRoot/check-prerequisites.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Prerequisites check failed" -ForegroundColor Red
    exit 1
}

# Get AWS account ID if not provided
if ([string]::IsNullOrEmpty($AwsAccountId)) {
    Write-Host "`n🔍 Getting AWS account ID..." -ForegroundColor Yellow
    $AwsAccountId = (aws sts get-caller-identity --query Account --output text)
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to get AWS account ID" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ AWS Account ID: $AwsAccountId" -ForegroundColor Green
}

# Get budget email if not provided
if ([string]::IsNullOrEmpty($BudgetEmail)) {
    $BudgetEmail = Read-Host "Enter email for budget alerts"
    if ([string]::IsNullOrEmpty($BudgetEmail)) {
        Write-Host "❌ Budget email is required" -ForegroundColor Red
        exit 1
    }
}

# Create terraform.tfvars
Write-Host "`n📝 Creating terraform.tfvars..." -ForegroundColor Yellow
$tfvarsContent = @"
project_name = "$ProjectName"
aws_region = "$AwsRegion"
aws_account_id = "$AwsAccountId"
cost_center = "$CostCenter"
budget_email = "$BudgetEmail"
"@

$tfvarsContent | Out-File -FilePath "terraform/setup/terraform.tfvars" -Encoding UTF8
Write-Host "✓ Created terraform.tfvars" -ForegroundColor Green

# Initialize Terraform
Write-Host "`n🔧 Initializing Terraform..." -ForegroundColor Yellow
Set-Location "terraform/setup"
terraform init
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Terraform init failed" -ForegroundColor Red
    exit 1
}

# Plan Terraform
Write-Host "`n📋 Planning Terraform..." -ForegroundColor Yellow
terraform plan -var-file="terraform.tfvars"
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Terraform plan failed" -ForegroundColor Red
    exit 1
}

# Apply Terraform
Write-Host "`n🚀 Applying Terraform..." -ForegroundColor Yellow
terraform apply -var-file="terraform.tfvars" -auto-approve
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Terraform apply failed" -ForegroundColor Red
    exit 1
}

# Get outputs
Write-Host "`n📤 Getting Terraform outputs..." -ForegroundColor Yellow
$outputs = terraform output -json | ConvertFrom-Json

# Create .env files for services
Write-Host "`n📝 Creating .env files..." -ForegroundColor Yellow

# Frontend .env
$frontendEnv = @"
VITE_API_URL=https://api-gateway.sri-subscription.com
VITE_COGNITO_USER_POOL_ID=$($outputs.cognito_user_pool_id.value)
VITE_COGNITO_CLIENT_ID=$($outputs.cognito_user_pool_client_id.value)
VITE_COGNITO_DOMAIN=$($outputs.cognito_domain.value)
"@
$frontendEnv | Out-File -FilePath "../frontend/.env" -Encoding UTF8

# Identity Service .env
$identityEnv = @"
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=sri-subscription_dev;Username=postgres;Password=postgres
ASPNETCORE_ENVIRONMENT=Development
"@
$identityEnv | Out-File -FilePath "../backend/IdentityService/.env" -Encoding UTF8

# API Gateway .env
$gatewayEnv = @"
Services__IdentityService=http://localhost:5001
Services__DataService=http://localhost:5003
ASPNETCORE_ENVIRONMENT=Development
"@
$gatewayEnv | Out-File -FilePath "../backend/ApiGateway/.env" -Encoding UTF8

# Data Service .env
$dataEnv = @"
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=sri-subscription_dev;Username=postgres;Password=postgres
ASPNETCORE_ENVIRONMENT=Development
"@
$dataEnv | Out-File -FilePath "../backend/DataService/.env" -Encoding UTF8

Write-Host "✓ Created .env files" -ForegroundColor Green

# Generate GitHub secrets instructions
Write-Host "`n📋 GitHub Secrets Setup Instructions:" -ForegroundColor Yellow
Write-Host "Add these secrets to your GitHub repository:"
Write-Host ""
Write-Host "AWS_ACCESS_KEY_ID=<your-access-key>"
Write-Host "AWS_SECRET_ACCESS_KEY=<your-secret-key>"
Write-Host "AWS_REGION=$AwsRegion"
Write-Host "ECR_IDENTITY_SERVICE_URL=$($outputs.ecr_repository_urls.value.identity_service)"
Write-Host "ECR_API_GATEWAY_URL=$($outputs.ecr_repository_urls.value.api_gateway)"
Write-Host "ECR_DATA_SERVICE_URL=$($outputs.ecr_repository_urls.value.data_service)"
Write-Host "ECR_FRONTEND_URL=$($outputs.ecr_repository_urls.value.frontend)"
Write-Host "S3_BUCKET_NAME=$($outputs.s3_bucket_name.value)"
Write-Host "DYNAMODB_TABLE_NAME=$($outputs.dynamodb_table_name.value)"
Write-Host "VPC_ID=$($outputs.vpc_id.value)"
Write-Host "PUBLIC_SUBNET_IDS=$($outputs.public_subnet_ids.value -join ',')"
Write-Host "APP_RUNNER_SECURITY_GROUP_ID=$($outputs.app_runner_security_group_id.value)"
Write-Host "RDS_SECURITY_GROUP_ID=$($outputs.rds_security_group_id.value)"
Write-Host "COGNITO_USER_POOL_ID=$($outputs.cognito_user_pool_id.value)"
Write-Host "COGNITO_USER_POOL_CLIENT_ID=$($outputs.cognito_user_pool_client_id.value)"
Write-Host "COGNITO_DOMAIN=$($outputs.cognito_domain.value)"

Write-Host "`n✅ AWS infrastructure setup completed!" -ForegroundColor Green
Write-Host "Next steps:"
Write-Host "1. Add GitHub secrets listed above"
Write-Host "2. Proceed to Phase 5: AWS App Deployment"





