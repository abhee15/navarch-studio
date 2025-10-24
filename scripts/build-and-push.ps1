# Build and Push Docker Images to ECR
# This script builds Docker images for all services and pushes them to ECR

param(
    [Parameter(Mandatory=$true)]
    [string]$Environment = "dev"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Build and Push Docker Images" -ForegroundColor Cyan
Write-Host "  Environment: $Environment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Get AWS Account ID
Write-Host "`n[1/6] Getting AWS Account ID..." -ForegroundColor Yellow
$AWS_ACCOUNT_ID = aws sts get-caller-identity --query Account --output text
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to get AWS Account ID" -ForegroundColor Red
    exit 1
}
Write-Host "AWS Account ID: $AWS_ACCOUNT_ID" -ForegroundColor Green

# Get AWS Region
$AWS_REGION = aws configure get region
if (-not $AWS_REGION) {
    $AWS_REGION = "us-east-1"
}
Write-Host "AWS Region: $AWS_REGION" -ForegroundColor Green

# ECR Login
Write-Host "`n[2/6] Logging into ECR..." -ForegroundColor Yellow
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin "$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to login to ECR" -ForegroundColor Red
    exit 1
}
Write-Host "Successfully logged into ECR" -ForegroundColor Green

# Get project name from terraform output
Write-Host "`n[3/6] Getting project configuration..." -ForegroundColor Yellow
Push-Location terraform/setup
$PROJECT_NAME = terraform output -raw s3_bucket_name | ForEach-Object { $_.Split('-')[0..($_.Split('-').Count-4)] -join '-' }
Pop-Location

if (-not $PROJECT_NAME) {
    Write-Host "Error: Could not determine project name from terraform output" -ForegroundColor Red
    exit 1
}
Write-Host "Project Name: $PROJECT_NAME" -ForegroundColor Green

# Build and Push Identity Service
Write-Host "`n[4/6] Building and pushing Identity Service..." -ForegroundColor Yellow
$IDENTITY_REPO = "$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$PROJECT_NAME-identity-service"
docker build -t $IDENTITY_REPO:latest -f backend/IdentityService/Dockerfile backend/
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to build Identity Service" -ForegroundColor Red
    exit 1
}
docker push $IDENTITY_REPO:latest
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to push Identity Service" -ForegroundColor Red
    exit 1
}
Write-Host "Identity Service pushed successfully" -ForegroundColor Green

# Build and Push API Gateway
Write-Host "`n[5/6] Building and pushing API Gateway..." -ForegroundColor Yellow
$API_GATEWAY_REPO = "$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$PROJECT_NAME-api-gateway"
docker build -t $API_GATEWAY_REPO:latest -f backend/ApiGateway/Dockerfile backend/
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to build API Gateway" -ForegroundColor Red
    exit 1
}
docker push $API_GATEWAY_REPO:latest
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to push API Gateway" -ForegroundColor Red
    exit 1
}
Write-Host "API Gateway pushed successfully" -ForegroundColor Green

# Build and Push Data Service
Write-Host "`n[6/6] Building and pushing Data Service..." -ForegroundColor Yellow
$DATA_SERVICE_REPO = "$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$PROJECT_NAME-data-service"
docker build -t $DATA_SERVICE_REPO:latest -f backend/DataService/Dockerfile backend/
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to build Data Service" -ForegroundColor Red
    exit 1
}
docker push $DATA_SERVICE_REPO:latest
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to push Data Service" -ForegroundColor Red
    exit 1
}
Write-Host "Data Service pushed successfully" -ForegroundColor Green

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  All images built and pushed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Deploy infrastructure: .\scripts\deploy.ps1 -Environment $Environment" -ForegroundColor White






