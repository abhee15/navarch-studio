# Script to create a test user in Cognito User Pool
# Usage: .\scripts\create-cognito-user.ps1 -Email "user@example.com" -Password "YourPassword123!"

param(
    [Parameter(Mandatory=$true)]
    [string]$Email,
    
    [Parameter(Mandatory=$true)]
    [string]$Password
)

$ErrorActionPreference = "Stop"

Write-Host "🔐 Creating Cognito User..." -ForegroundColor Cyan

# Get Cognito User Pool ID from Terraform
Set-Location "$PSScriptRoot\..\terraform\setup"
$userPoolId = terraform output -raw cognito_user_pool_id
$region = "us-east-1"

if ([string]::IsNullOrEmpty($userPoolId)) {
    Write-Host "❌ Could not get Cognito User Pool ID from Terraform" -ForegroundColor Red
    exit 1
}

Write-Host "User Pool ID: $userPoolId" -ForegroundColor Gray
Write-Host "Region: $region" -ForegroundColor Gray
Write-Host "Email: $Email" -ForegroundColor Gray

# Create user
Write-Host "`nCreating user in Cognito..." -ForegroundColor Yellow
aws cognito-idp admin-create-user `
    --user-pool-id $userPoolId `
    --username $Email `
    --user-attributes Name=email,Value=$Email Name=email_verified,Value=true `
    --temporary-password "TempPass123!" `
    --message-action SUPPRESS `
    --region $region

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to create user" -ForegroundColor Red
    exit 1
}

# Set permanent password
Write-Host "Setting permanent password..." -ForegroundColor Yellow
aws cognito-idp admin-set-user-password `
    --user-pool-id $userPoolId `
    --username $Email `
    --password $Password `
    --permanent `
    --region $region

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to set password" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ User created successfully!" -ForegroundColor Green
Write-Host "`n📋 Login Credentials:" -ForegroundColor Cyan
Write-Host "   Email: $Email" -ForegroundColor White
Write-Host "   Password: $Password" -ForegroundColor White
Write-Host "`n🌐 You can now login at your frontend URL" -ForegroundColor Green

