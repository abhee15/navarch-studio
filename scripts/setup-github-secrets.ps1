#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Automatically configure all required GitHub Secrets from Terraform outputs

.DESCRIPTION
    This script reads Terraform outputs from terraform/setup and configures
    all required GitHub Secrets for CI/CD workflows. It checks which secrets
    already exist and only sets missing ones.

.PARAMETER Force
    Force update of all secrets, even if they already exist

.EXAMPLE
    .\setup-github-secrets.ps1
    
.EXAMPLE
    .\setup-github-secrets.ps1 -Force
#>

param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Check if gh CLI is installed
if (!(Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "❌ GitHub CLI (gh) not found!" -ForegroundColor Red
    Write-Host "📥 Install from: https://cli.github.com/" -ForegroundColor Yellow
    exit 1
}

# Check if logged in to gh
$ghAuthStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Not logged in to GitHub CLI" -ForegroundColor Red
    Write-Host "🔑 Run: gh auth login" -ForegroundColor Yellow
    exit 1
}

Write-Host "🔐 GitHub Secrets Setup Script" -ForegroundColor Cyan
Write-Host "=" * 50

# Function to check if a secret exists
function Test-SecretExists {
    param([string]$SecretName)
    
    $secrets = gh secret list 2>&1 | Out-String
    return $secrets -match $SecretName
}

# Function to set a secret
function Set-GitHubSecret {
    param(
        [string]$Name,
        [string]$Value,
        [switch]$Required = $true
    )
    
    if ([string]::IsNullOrWhiteSpace($Value)) {
        if ($Required) {
            Write-Host "⚠️  $Name - EMPTY (required!)" -ForegroundColor Yellow
        } else {
            Write-Host "⏭️  $Name - SKIPPED (optional)" -ForegroundColor Gray
        }
        return $false
    }
    
    $exists = Test-SecretExists -SecretName $Name
    
    if ($exists -and !$Force) {
        Write-Host "✓  $Name - EXISTS (use -Force to update)" -ForegroundColor Green
        return $true
    }
    
    try {
        $Value | gh secret set $Name --body -
        if ($LASTEXITCODE -eq 0) {
            if ($exists) {
                Write-Host "✓  $Name - UPDATED" -ForegroundColor Cyan
            } else {
                Write-Host "✓  $Name - SET" -ForegroundColor Green
            }
            return $true
        } else {
            Write-Host "❌ $Name - FAILED" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "❌ $Name - ERROR: $_" -ForegroundColor Red
        return $false
    }
}

# Check prerequisites
Write-Host "`n📋 Checking Prerequisites..." -ForegroundColor Cyan

# Check if in correct directory
if (!(Test-Path "terraform/setup")) {
    Write-Host "❌ Must run from project root directory" -ForegroundColor Red
    exit 1
}

# Check if Terraform state exists
if (!(Test-Path "terraform/setup/terraform.tfstate")) {
    Write-Host "❌ Terraform setup not run yet!" -ForegroundColor Red
    Write-Host "📝 Please run first:" -ForegroundColor Yellow
    Write-Host "   cd terraform/setup" -ForegroundColor Gray
    Write-Host "   terraform init" -ForegroundColor Gray
    Write-Host "   terraform apply" -ForegroundColor Gray
    exit 1
}

Write-Host "✓ Prerequisites OK`n" -ForegroundColor Green

# Step 1: Project Configuration (Manual)
Write-Host "📝 Step 1: Project Configuration" -ForegroundColor Cyan
Write-Host "These need to be set manually (if not already set):`n"

$projectName = Read-Host "Enter PROJECT_NAME (e.g., navarch-studio)"
$costCenter = Read-Host "Enter COST_CENTER (e.g., engineering)"
$domainName = Read-Host "Enter DOMAIN_NAME (leave empty if none)"

$set = 0
$set += Set-GitHubSecret -Name "PROJECT_NAME" -Value $projectName ? 1 : 0
$set += Set-GitHubSecret -Name "COST_CENTER" -Value $costCenter ? 1 : 0
$set += Set-GitHubSecret -Name "DOMAIN_NAME" -Value $domainName -Required:$false ? 1 : 0

Write-Host "`n✓ Project configuration: $set/3 set`n" -ForegroundColor Green

# Step 2: Get Terraform Outputs
Write-Host "📦 Step 2: Reading Terraform Outputs..." -ForegroundColor Cyan

Push-Location terraform/setup

try {
    $terraformOutputs = @{}
    
    # Get all outputs as JSON
    $outputJson = terraform output -json | ConvertFrom-Json
    
    foreach ($key in $outputJson.PSObject.Properties.Name) {
        $terraformOutputs[$key] = $outputJson.$key.value
    }
    
    Write-Host "✓ Terraform outputs loaded`n" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Failed to read Terraform outputs: $_" -ForegroundColor Red
    Pop-Location
    exit 1
}

Pop-Location

# Step 3: Infrastructure IDs
Write-Host "🏗️  Step 3: Infrastructure IDs" -ForegroundColor Cyan

$set = 0
$set += Set-GitHubSecret -Name "S3_BUCKET_NAME" -Value $terraformOutputs.s3_bucket_name ? 1 : 0
$set += Set-GitHubSecret -Name "DYNAMODB_TABLE_NAME" -Value $terraformOutputs.dynamodb_table_name ? 1 : 0
$set += Set-GitHubSecret -Name "VPC_ID" -Value $terraformOutputs.vpc_id ? 1 : 0
$set += Set-GitHubSecret -Name "PUBLIC_SUBNET_IDS" -Value ($terraformOutputs.public_subnet_ids | ConvertTo-Json -Compress) ? 1 : 0
$set += Set-GitHubSecret -Name "APP_RUNNER_SECURITY_GROUP_ID" -Value $terraformOutputs.app_runner_security_group_id ? 1 : 0
$set += Set-GitHubSecret -Name "RDS_SECURITY_GROUP_ID" -Value $terraformOutputs.rds_security_group_id ? 1 : 0

Write-Host "`n✓ Infrastructure: $set/6 set`n" -ForegroundColor Green

# Step 4: ECR Repository URLs
Write-Host "🐳 Step 4: ECR Repository URLs" -ForegroundColor Cyan

$ecrUrls = $terraformOutputs.ecr_repository_urls
$set = 0
$set += Set-GitHubSecret -Name "ECR_IDENTITY_SERVICE_URL" -Value $ecrUrls.identity_service ? 1 : 0
$set += Set-GitHubSecret -Name "ECR_API_GATEWAY_URL" -Value $ecrUrls.api_gateway ? 1 : 0
$set += Set-GitHubSecret -Name "ECR_DATA_SERVICE_URL" -Value $ecrUrls.data_service ? 1 : 0
$set += Set-GitHubSecret -Name "ECR_FRONTEND_URL" -Value $ecrUrls.frontend ? 1 : 0

Write-Host "`n✓ ECR URLs: $set/4 set`n" -ForegroundColor Green

# Step 5: Cognito Configuration
Write-Host "🔐 Step 5: Cognito Configuration" -ForegroundColor Cyan

$cognitoDomain = $terraformOutputs.cognito_domain
if (!$cognitoDomain) {
    $cognitoDomain = ""  # Optional
}

$set = 0
$set += Set-GitHubSecret -Name "COGNITO_USER_POOL_ID" -Value $terraformOutputs.cognito_user_pool_id ? 1 : 0
$set += Set-GitHubSecret -Name "COGNITO_USER_POOL_CLIENT_ID" -Value $terraformOutputs.cognito_user_pool_client_id ? 1 : 0
$set += Set-GitHubSecret -Name "COGNITO_DOMAIN" -Value $cognitoDomain -Required:$false ? 1 : 0

Write-Host "`n✓ Cognito: $set/3 set`n" -ForegroundColor Green

# Step 6: Database Configuration (Manual)
Write-Host "🗄️  Step 6: Database Configuration" -ForegroundColor Cyan
Write-Host "Enter your database credentials:`n"

$rdsDatabase = Read-Host "Enter RDS_DATABASE (default: navarch_studio_db)"
if ([string]::IsNullOrWhiteSpace($rdsDatabase)) {
    $rdsDatabase = "navarch_studio_db"
}

$rdsUsername = Read-Host "Enter RDS_USERNAME (default: postgres)"
if ([string]::IsNullOrWhiteSpace($rdsUsername)) {
    $rdsUsername = "postgres"
}

$rdsPassword = Read-Host "Enter RDS_PASSWORD" -AsSecureString
$rdsPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($rdsPassword))

$set = 0
$set += Set-GitHubSecret -Name "RDS_DATABASE" -Value $rdsDatabase ? 1 : 0
$set += Set-GitHubSecret -Name "RDS_USERNAME" -Value $rdsUsername ? 1 : 0
$set += Set-GitHubSecret -Name "RDS_PASSWORD" -Value $rdsPasswordPlain ? 1 : 0

Write-Host "`n✓ Database: $set/3 set`n" -ForegroundColor Green

# Summary
Write-Host "`n" + ("=" * 50) -ForegroundColor Cyan
Write-Host "✅ GitHub Secrets Setup Complete!" -ForegroundColor Green
Write-Host "`n📊 Summary:" -ForegroundColor Cyan
Write-Host "   • List secrets: gh secret list" -ForegroundColor Gray
Write-Host "   • View workflow: .github/workflows/ci-dev.yml" -ForegroundColor Gray
Write-Host "   • Trigger deployment: git push origin main" -ForegroundColor Gray
Write-Host "`n🚀 Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Verify all secrets: gh secret list" -ForegroundColor White
Write-Host "   2. Commit and push to trigger CI/CD" -ForegroundColor White
Write-Host "   3. Monitor workflow: gh run list --workflow=ci-dev.yml" -ForegroundColor White


