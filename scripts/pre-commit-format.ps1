#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Pre-commit hook to automatically format code before committing.
    
.DESCRIPTION
    This script runs dotnet format on backend code and prettier on frontend code
    to ensure all commits meet formatting standards.
#>

Write-Host "🔍 Running pre-commit formatting checks..." -ForegroundColor Cyan

$hasErrors = $false

# Check if there are any .cs files staged
$csFiles = git diff --cached --name-only --diff-filter=ACM | Where-Object { $_ -like "*.cs" }

if ($csFiles) {
    Write-Host "📝 Formatting .NET code..." -ForegroundColor Yellow
    
    Push-Location backend
    
    # Run dotnet format
    $formatResult = dotnet format --verify-no-changes --verbosity quiet
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠️  .NET code needs formatting. Running dotnet format..." -ForegroundColor Yellow
        dotnet format
        
        # Add formatted files back to staging
        Pop-Location
        foreach ($file in $csFiles) {
            git add $file
        }
        Push-Location backend
        
        Write-Host "✅ .NET code formatted and re-staged" -ForegroundColor Green
    } else {
        Write-Host "✅ .NET code is properly formatted" -ForegroundColor Green
    }
    
    Pop-Location
}

# Check if there are any TypeScript/JavaScript files staged
$frontendFiles = git diff --cached --name-only --diff-filter=ACM | Where-Object { 
    $_ -like "*.ts" -or $_ -like "*.tsx" -or $_ -like "*.js" -or $_ -like "*.jsx"
}

if ($frontendFiles -and (Test-Path "frontend")) {
    Write-Host "📝 Formatting frontend code..." -ForegroundColor Yellow
    
    Push-Location frontend
    
    # Check if node_modules exists
    if (Test-Path "node_modules") {
        # Run prettier
        npm run format-check 2>&1 | Out-Null
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "⚠️  Frontend code needs formatting. Running prettier..." -ForegroundColor Yellow
            npm run format
            
            # Add formatted files back to staging
            Pop-Location
            foreach ($file in $frontendFiles) {
                git add $file
            }
            Push-Location frontend
            
            Write-Host "✅ Frontend code formatted and re-staged" -ForegroundColor Green
        } else {
            Write-Host "✅ Frontend code is properly formatted" -ForegroundColor Green
        }
    } else {
        Write-Host "⚠️  node_modules not found. Run 'npm install' in frontend folder." -ForegroundColor Yellow
    }
    
    Pop-Location
}

if ($hasErrors) {
    Write-Host "❌ Pre-commit checks failed" -ForegroundColor Red
    exit 1
}

Write-Host "✅ All pre-commit checks passed!" -ForegroundColor Green
exit 0

