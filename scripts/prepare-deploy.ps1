# Prepare Deploy Configuration
# This script creates terraform.tfvars and backend-config.tfvars from setup outputs

param(
    [Parameter(Mandatory=$false)]
    [string]$ProjectName = ""
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Prepare Deploy Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Navigate to setup directory to get outputs
Write-Host "`n[1/3] Reading setup outputs..." -ForegroundColor Yellow
Push-Location terraform/setup

# Get outputs
try {
    $vpc_id = terraform output -raw vpc_id
    $public_subnet_ids_json = terraform output -json public_subnet_ids | ConvertFrom-Json
    $public_subnet_ids = $public_subnet_ids_json -join '", "'
    $app_runner_sg = terraform output -raw app_runner_security_group_id
    $rds_sg = terraform output -raw rds_security_group_id
    $ecr_urls_json = terraform output -json ecr_repository_urls | ConvertFrom-Json
    $cognito_pool_id = terraform output -raw cognito_user_pool_id
    $cognito_client_id = terraform output -raw cognito_user_pool_client_id
    $cognito_domain = terraform output -raw cognito_domain
    $s3_bucket = terraform output -raw s3_bucket_name
    $dynamodb_table = terraform output -raw dynamodb_table_name
    $aws_region = aws configure get region
    if (-not $aws_region) { $aws_region = "us-east-1" }
    
    Write-Host "✓ Successfully read setup outputs" -ForegroundColor Green
} catch {
    Write-Host "Error: Failed to read setup outputs" -ForegroundColor Red
    Write-Host "Make sure Phase 4 setup is complete and terraform outputs are available" -ForegroundColor Yellow
    Pop-Location
    exit 1
}

Pop-Location

# Get project name from bucket name if not provided
if (-not $ProjectName) {
    $ProjectName = $s3_bucket -replace '-terraform-state-\d+$', ''
}
Write-Host "Project Name: $ProjectName" -ForegroundColor Green

# Create backend-config.tfvars
Write-Host "`n[2/3] Creating backend-config.tfvars..." -ForegroundColor Yellow
$backendConfig = @"
# Backend Configuration (auto-generated from setup outputs)
# Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

bucket         = "$s3_bucket"
region         = "$aws_region"
dynamodb_table = "$dynamodb_table"
"@

Set-Content -Path "terraform/deploy/backend-config.tfvars" -Value $backendConfig
Write-Host "✓ Created terraform/deploy/backend-config.tfvars" -ForegroundColor Green

# Create terraform.tfvars
Write-Host "`n[3/3] Creating terraform.tfvars..." -ForegroundColor Yellow
$terraformVars = @"
# Terraform Variables (auto-generated from setup outputs)
# Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

project_name = "$ProjectName"
aws_region   = "$aws_region"
cost_center  = "engineering"

# From Phase 4 Setup
vpc_id                        = "$vpc_id"
public_subnet_ids             = ["$public_subnet_ids"]
app_runner_security_group_id  = "$app_runner_sg"
rds_security_group_id         = "$rds_sg"

ecr_repository_urls = {
  identity_service = "$($ecr_urls_json.identity_service)"
  api_gateway      = "$($ecr_urls_json.api_gateway)"
  data_service     = "$($ecr_urls_json.data_service)"
  frontend         = "$($ecr_urls_json.frontend)"
}

cognito_user_pool_id        = "$cognito_pool_id"
cognito_user_pool_client_id = "$cognito_client_id"
cognito_domain              = "$cognito_domain"
"@

Set-Content -Path "terraform/deploy/terraform.tfvars" -Value $terraformVars
Write-Host "✓ Created terraform/deploy/terraform.tfvars" -ForegroundColor Green

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Configuration Ready!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Review terraform/deploy/terraform.tfvars" -ForegroundColor White
Write-Host "2. Review terraform/deploy/backend-config.tfvars" -ForegroundColor White
Write-Host "3. Build and push Docker images: .\scripts\build-and-push.ps1 -Environment dev" -ForegroundColor White
Write-Host "4. Deploy infrastructure: .\scripts\deploy.ps1 -Environment dev" -ForegroundColor White






