# Deploy Infrastructure to AWS
# This script deploys the application infrastructure using Terraform

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment = "dev",
    
    [Parameter(Mandatory=$false)]
    [switch]$AutoApprove = $false
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Deploy to AWS" -ForegroundColor Cyan
Write-Host "  Environment: $Environment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Check prerequisites
Write-Host "`n[1/7] Checking prerequisites..." -ForegroundColor Yellow

# Check if terraform is installed
if (-not (Get-Command terraform -ErrorAction SilentlyContinue)) {
    Write-Host "Error: Terraform is not installed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Terraform installed" -ForegroundColor Green

# Check if AWS CLI is installed
if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
    Write-Host "Error: AWS CLI is not installed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ AWS CLI installed" -ForegroundColor Green

# Check AWS credentials
$AWS_ACCOUNT = aws sts get-caller-identity --query Account --output text 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: AWS credentials not configured" -ForegroundColor Red
    exit 1
}
Write-Host "✓ AWS credentials configured (Account: $AWS_ACCOUNT)" -ForegroundColor Green

# Navigate to deploy directory
Write-Host "`n[2/7] Navigating to deploy directory..." -ForegroundColor Yellow
Push-Location terraform/deploy

# Check if configuration files exist
Write-Host "`n[3/7] Checking configuration files..." -ForegroundColor Yellow

if (-not (Test-Path "terraform.tfvars")) {
    Write-Host "Error: terraform.tfvars not found" -ForegroundColor Red
    Write-Host "Please copy terraform.tfvars.example to terraform.tfvars and update with your values" -ForegroundColor Yellow
    Pop-Location
    exit 1
}
Write-Host "✓ terraform.tfvars found" -ForegroundColor Green

if (-not (Test-Path "backend-config.tfvars")) {
    Write-Host "Error: backend-config.tfvars not found" -ForegroundColor Red
    Write-Host "Please copy backend-config.tfvars.example to backend-config.tfvars and update with your values" -ForegroundColor Yellow
    Pop-Location
    exit 1
}
Write-Host "✓ backend-config.tfvars found" -ForegroundColor Green

if (-not (Test-Path "environments/$Environment.tfvars")) {
    Write-Host "Error: environments/$Environment.tfvars not found" -ForegroundColor Red
    Pop-Location
    exit 1
}
Write-Host "✓ environments/$Environment.tfvars found" -ForegroundColor Green

# Initialize Terraform
Write-Host "`n[4/7] Initializing Terraform..." -ForegroundColor Yellow
terraform init -backend-config=backend-config.tfvars
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Terraform init failed" -ForegroundColor Red
    Pop-Location
    exit 1
}
Write-Host "Terraform initialized successfully" -ForegroundColor Green

# Validate Terraform configuration
Write-Host "`n[5/7] Validating Terraform configuration..." -ForegroundColor Yellow
terraform validate
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Terraform validation failed" -ForegroundColor Red
    Pop-Location
    exit 1
}
Write-Host "Terraform configuration is valid" -ForegroundColor Green

# Plan deployment
Write-Host "`n[6/7] Planning deployment..." -ForegroundColor Yellow
terraform plan `
    -var-file=terraform.tfvars `
    -var-file=environments/$Environment.tfvars `
    -out=tfplan
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Terraform plan failed" -ForegroundColor Red
    Pop-Location
    exit 1
}
Write-Host "Terraform plan completed" -ForegroundColor Green

# Apply deployment
Write-Host "`n[7/7] Applying deployment..." -ForegroundColor Yellow
if ($AutoApprove) {
    terraform apply -auto-approve tfplan
} else {
    terraform apply tfplan
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Terraform apply failed" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

# Show outputs
Write-Host "`nDeployment Summary:" -ForegroundColor Yellow
terraform output deployment_summary

Write-Host "`nService URLs:" -ForegroundColor Yellow
Write-Host "Identity Service: " -NoNewline; terraform output -raw identity_service_url
Write-Host "API Gateway: " -NoNewline; terraform output -raw api_gateway_url
Write-Host "Data Service: " -NoNewline; terraform output -raw data_service_url
Write-Host "Frontend: " -NoNewline; terraform output -raw frontend_url

Pop-Location

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Run database migrations" -ForegroundColor White
Write-Host "2. Build and deploy frontend" -ForegroundColor White
Write-Host "3. Test the application" -ForegroundColor White






