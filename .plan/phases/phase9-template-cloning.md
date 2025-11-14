# Phase 9: Template Cloning System

## Goal

Make it trivial to create new projects from this template with minimal manual steps and maximum automation.

## Prerequisites

- Phase 8 completed (polish and documentation)
- Understanding of template cloning patterns
- GitHub CLI installed (optional)

## Deliverables Checklist

### 1. Clone Template Script

#### Interactive Clone Script

**File**: `scripts/clone-template.ps1`

````powershell
# Interactive template cloning script
param(
    [string]$ConfigFile = ""
)

Write-Host "🚀 Full Stack Template Cloner" -ForegroundColor Green
Write-Host "This script will create a new project from this template" -ForegroundColor Yellow

# Check if config file provided
if ($ConfigFile -and (Test-Path $ConfigFile)) {
    Write-Host "📄 Using config file: $ConfigFile" -ForegroundColor Yellow
    $config = Get-Content $ConfigFile | ConvertFrom-Json
} else {
    Write-Host "📝 Interactive setup" -ForegroundColor Yellow
    $config = @{}
}

# Collect project information
$config.ProjectName = if ($config.ProjectName) { $config.ProjectName } else {
    Read-Host "Enter project name (lowercase, no spaces)"
}

$config.AwsRegion = if ($config.AwsRegion) { $config.AwsRegion } else {
    Read-Host "Enter AWS region [us-east-1]"
    if ([string]::IsNullOrEmpty($config.AwsRegion)) { $config.AwsRegion = "us-east-1" }
}

$config.AwsAccountId = if ($config.AwsAccountId) { $config.AwsAccountId } else {
    Read-Host "Enter AWS account ID"
}

$config.GitHubOwner = if ($config.GitHubOwner) { $config.GitHubOwner } else {
    Read-Host "Enter GitHub username/organization"
}

$config.DomainName = if ($config.DomainName) { $config.DomainName } else {
    $domain = Read-Host "Enter custom domain (optional, press Enter to skip)"
    if ([string]::IsNullOrEmpty($domain)) { $domain = "" }
    $domain
}

$config.BudgetEmail = if ($config.BudgetEmail) { $config.BudgetEmail } else {
    Read-Host "Enter email for budget alerts"
}

$config.CostCenter = if ($config.CostCenter) { $config.CostCenter } else {
    Read-Host "Enter cost center [engineering]"
    if ([string]::IsNullOrEmpty($config.CostCenter)) { $config.CostCenter = "engineering" }
}

# Validate inputs
if ([string]::IsNullOrEmpty($config.ProjectName) -or
    [string]::IsNullOrEmpty($config.AwsAccountId) -or
    [string]::IsNullOrEmpty($config.GitHubOwner) -or
    [string]::IsNullOrEmpty($config.BudgetEmail)) {
    Write-Host "❌ Required fields missing" -ForegroundColor Red
    exit 1
}

# Create new project directory
$projectDir = "../$($config.ProjectName)"
if (Test-Path $projectDir) {
    Write-Host "❌ Directory $projectDir already exists" -ForegroundColor Red
    exit 1
}

Write-Host "📁 Creating project directory: $projectDir" -ForegroundColor Yellow
New-Item -ItemType Directory -Path $projectDir -Force

# Copy template files
Write-Host "📋 Copying template files..." -ForegroundColor Yellow
Copy-Item -Path "." -Destination $projectDir -Recurse -Exclude @(".git", "node_modules", "bin", "obj", "*.tfstate*", ".terraform")

# Replace placeholders
Write-Host "🔄 Replacing placeholders..." -ForegroundColor Yellow
$files = Get-ChildItem -Path $projectDir -Recurse -File
foreach ($file in $files) {
    if ($file.Extension -in @(".md", ".json", ".yml", ".yaml", ".tf", ".cs", ".ts", ".tsx", ".js", ".jsx", ".ps1", ".sh")) {
        $content = Get-Content $file.FullName -Raw
        $content = $content -replace "sri-subscription", $config.ProjectName
        $content = $content -replace "us-east-1", $config.AwsRegion
        $content = $content -replace "344870914438", $config.AwsAccountId
        $content = $content -replace "abhee15", $config.GitHubOwner
        $content = $content -replace "", $config.DomainName
        $content = $content -replace "{{BUDGET_EMAIL}}", $config.BudgetEmail
        $content = $content -replace "{{COST_CENTER}}", $config.CostCenter
        Set-Content $file.FullName -Value $content
    }
}

# Initialize Git repository
Write-Host "🔧 Initializing Git repository..." -ForegroundColor Yellow
Set-Location $projectDir
git init
git add .
git commit -m "Initial commit from template"

# Create GitHub repository (optional)
$createRepo = Read-Host "Create GitHub repository? (y/N)"
if ($createRepo -eq "y" -or $createRepo -eq "Y") {
    Write-Host "🔗 Creating GitHub repository..." -ForegroundColor Yellow
    gh repo create $config.ProjectName --public --source=. --remote=origin --push
} else {
    Write-Host "📝 Manual GitHub setup required:" -ForegroundColor Yellow
    Write-Host "1. Create repository on GitHub: https://github.com/new"
    Write-Host "2. Add remote: git remote add origin https://github.com/$($config.GitHubOwner)/$($config.ProjectName).git"
    Write-Host "3. Push: git push -u origin main"
}

# Generate setup instructions
Write-Host "📋 Generating setup instructions..." -ForegroundColor Yellow
$setupInstructions = @"
# Setup Instructions for $($config.ProjectName)

## 1. Prerequisites
- Node.js 20+
- .NET 8 SDK
- Docker Desktop
- AWS CLI
- Terraform

## 2. AWS Setup
```bash
# Configure AWS CLI
aws configure

# Run setup script
./scripts/setup.ps1
````

## 3. GitHub Secrets

Add these secrets to your GitHub repository:

- AWS_ACCESS_KEY_ID
- AWS_SECRET_ACCESS_KEY
- AWS_REGION
- ECR_IDENTITY_SERVICE_URL
- ECR_API_GATEWAY_URL
- ECR_DATA_SERVICE_URL
- ECR_FRONTEND_URL
- S3_BUCKET_NAME
- DYNAMODB_TABLE_NAME
- VPC_ID
- PUBLIC_SUBNET_IDS
- PRIVATE_SUBNET_IDS
- APP_RUNNER_SECURITY_GROUP_ID
- RDS_SECURITY_GROUP_ID
- COGNITO_USER_POOL_ID
- COGNITO_USER_POOL_CLIENT_ID
- COGNITO_DOMAIN

## 4. Local Development

```bash
# Start local stack
docker-compose up

# Access services
# Frontend: http://localhost:3000
# API Gateway: http://localhost:5002/swagger
# PgAdmin: http://localhost:5050
```

## 5. Deployment

```bash
# Deploy to dev
./scripts/deploy-dev.ps1

# Deploy to staging
./scripts/deploy-staging.ps1

# Deploy to production
./scripts/deploy-prod.ps1
```

## 6. Documentation

- [Setup Guide](docs/SETUP.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Development Guide](docs/DEVELOPMENT.md)
- [Deployment Guide](docs/DEPLOYMENT.md)
- [Cost Estimates](docs/COST-ESTIMATE.md)
  "@

$setupInstructions | Out-File -FilePath "SETUP_INSTRUCTIONS.md" -Encoding UTF8

Write-Host "✅ Project created successfully!" -ForegroundColor Green
Write-Host "📁 Project directory: $projectDir" -ForegroundColor Yellow
Write-Host "📋 Setup instructions: $projectDir/SETUP_INSTRUCTIONS.md" -ForegroundColor Yellow
Write-Host "🚀 Next steps:" -ForegroundColor Yellow
Write-Host "1. cd $projectDir" -ForegroundColor White
Write-Host "2. ./scripts/setup.ps1" -ForegroundColor White
Write-Host "3. Follow setup instructions" -ForegroundColor White

````

#### Linux/Mac Clone Script
**File**: `scripts/clone-template.sh`
```bash
#!/bin/bash
# Interactive template cloning script for Linux/Mac

echo "🚀 Full Stack Template Cloner"
echo "This script will create a new project from this template"

# Collect project information
read -p "Enter project name (lowercase, no spaces): " PROJECT_NAME
read -p "Enter AWS region [us-east-1]: " AWS_REGION
read -p "Enter AWS account ID: " AWS_ACCOUNT_ID
read -p "Enter GitHub username/organization: " GITHUB_OWNER
read -p "Enter custom domain (optional, press Enter to skip): " DOMAIN_NAME
read -p "Enter email for budget alerts: " BUDGET_EMAIL
read -p "Enter cost center [engineering]: " COST_CENTER

# Set defaults
AWS_REGION=${AWS_REGION:-us-east-1}
COST_CENTER=${COST_CENTER:-engineering}

# Validate inputs
if [ -z "$PROJECT_NAME" ] || [ -z "$AWS_ACCOUNT_ID" ] || [ -z "$GITHUB_OWNER" ] || [ -z "$BUDGET_EMAIL" ]; then
    echo "❌ Required fields missing"
    exit 1
fi

# Create new project directory
PROJECT_DIR="../$PROJECT_NAME"
if [ -d "$PROJECT_DIR" ]; then
    echo "❌ Directory $PROJECT_DIR already exists"
    exit 1
fi

echo "📁 Creating project directory: $PROJECT_DIR"
mkdir -p "$PROJECT_DIR"

# Copy template files
echo "📋 Copying template files..."
cp -r . "$PROJECT_DIR"/
rm -rf "$PROJECT_DIR/.git"
rm -rf "$PROJECT_DIR/node_modules"
rm -rf "$PROJECT_DIR/bin"
rm -rf "$PROJECT_DIR/obj"
rm -rf "$PROJECT_DIR/*.tfstate*"
rm -rf "$PROJECT_DIR/.terraform"

# Replace placeholders
echo "🔄 Replacing placeholders..."
find "$PROJECT_DIR" -type f \( -name "*.md" -o -name "*.json" -o -name "*.yml" -o -name "*.yaml" -o -name "*.tf" -o -name "*.cs" -o -name "*.ts" -o -name "*.tsx" -o -name "*.js" -o -name "*.jsx" -o -name "*.ps1" -o -name "*.sh" \) -exec sed -i "s/sri-subscription/$PROJECT_NAME/g" {} \;
find "$PROJECT_DIR" -type f \( -name "*.md" -o -name "*.json" -o -name "*.yml" -o -name "*.yaml" -o -name "*.tf" -o -name "*.cs" -o -name "*.ts" -o -name "*.tsx" -o -name "*.js" -o -name "*.jsx" -o -name "*.ps1" -o -name "*.sh" \) -exec sed -i "s/us-east-1/$AWS_REGION/g" {} \;
find "$PROJECT_DIR" -type f \( -name "*.md" -o -name "*.json" -o -name "*.yml" -o -name "*.yaml" -o -name "*.tf" -o -name "*.cs" -o -name "*.ts" -o -name "*.tsx" -o -name "*.js" -o -name "*.jsx" -o -name "*.ps1" -o -name "*.sh" \) -exec sed -i "s/344870914438/$AWS_ACCOUNT_ID/g" {} \;
find "$PROJECT_DIR" -type f \( -name "*.md" -o -name "*.json" -o -name "*.yml" -o -name "*.yaml" -o -name "*.tf" -o -name "*.cs" -o -name "*.ts" -o -name "*.tsx" -o -name "*.js" -o -name "*.jsx" -o -name "*.ps1" -o -name "*.sh" \) -exec sed -i "s/abhee15/$GITHUB_OWNER/g" {} \;
find "$PROJECT_DIR" -type f \( -name "*.md" -o -name "*.json" -o -name "*.yml" -o -name "*.yaml" -o -name "*.tf" -o -name "*.cs" -o -name "*.ts" -o -name "*.tsx" -o -name "*.js" -o -name "*.jsx" -o -name "*.ps1" -o -name "*.sh" \) -exec sed -i "s//$DOMAIN_NAME/g" {} \;
find "$PROJECT_DIR" -type f \( -name "*.md" -o -name "*.json" -o -name "*.yml" -o -name "*.yaml" -o -name "*.tf" -o -name "*.cs" -o -name "*.ts" -o -name "*.tsx" -o -name "*.js" -o -name "*.jsx" -o -name "*.ps1" -o -name "*.sh" \) -exec sed -i "s/{{BUDGET_EMAIL}}/$BUDGET_EMAIL/g" {} \;
find "$PROJECT_DIR" -type f \( -name "*.md" -o -name "*.json" -o -name "*.yml" -o -name "*.yaml" -o -name "*.tf" -o -name "*.cs" -o -name "*.ts" -o -name "*.tsx" -o -name "*.js" -o -name "*.jsx" -o -name "*.ps1" -o -name "*.sh" \) -exec sed -i "s/{{COST_CENTER}}/$COST_CENTER/g" {} \;

# Initialize Git repository
echo "🔧 Initializing Git repository..."
cd "$PROJECT_DIR"
git init
git add .
git commit -m "Initial commit from template"

# Create GitHub repository (optional)
read -p "Create GitHub repository? (y/N): " CREATE_REPO
if [ "$CREATE_REPO" = "y" ] || [ "$CREATE_REPO" = "Y" ]; then
    echo "🔗 Creating GitHub repository..."
    gh repo create "$PROJECT_NAME" --public --source=. --remote=origin --push
else
    echo "📝 Manual GitHub setup required:"
    echo "1. Create repository on GitHub: https://github.com/new"
    echo "2. Add remote: git remote add origin https://github.com/$GITHUB_OWNER/$PROJECT_NAME.git"
    echo "3. Push: git push -u origin main"
fi

echo "✅ Project created successfully!"
echo "📁 Project directory: $PROJECT_DIR"
echo "🚀 Next steps:"
echo "1. cd $PROJECT_DIR"
echo "2. ./scripts/setup.sh"
echo "3. Follow setup instructions"
````

### 2. Configuration File Template

**File**: `template-config.yml.example`

```yaml
# Template Configuration File
# Copy this file to template-config.yml and fill in your values

project:
  name: "my-awesome-app" # Project name (lowercase, no spaces)
  description: "My awesome application"

aws:
  region: "us-east-1" # AWS region
  account_id: "123456789012" # AWS account ID
  budget_email: "admin@company.com" # Email for budget alerts
  cost_center: "engineering" # Cost center for billing

github:
  owner: "myusername" # GitHub username or organization
  repo: "my-awesome-app" # Repository name (usually same as project name)

domain:
  name: "myapp.com" # Custom domain (optional)
  subdomain: "api" # API subdomain (optional)

database:
  name: "myapp_dev" # Database name
  username: "postgres" # Database username

services:
  identity:
    name: "identity-service"
    port: 5001
  gateway:
    name: "api-gateway"
    port: 5002
  data:
    name: "data-service"
    port: 5003

environments:
  dev:
    db_instance_class: "db.t3.micro"
    enable_multi_az: false
    backup_retention_days: 7
  staging:
    db_instance_class: "db.t3.small"
    enable_multi_az: false
    backup_retention_days: 14
  prod:
    db_instance_class: "db.t3.medium"
    enable_multi_az: true
    backup_retention_days: 30
```

### 3. Cloning Documentation

**File**: `CLONING.md`

````markdown
# Template Cloning Guide

This guide explains how to create a new project from this template.

## Quick Start

### Method 1: Interactive Script (Recommended)

```bash
# Windows
./scripts/clone-template.ps1

# Linux/Mac
./scripts/clone-template.sh
```
````

### Method 2: Configuration File

```bash
# 1. Copy config template
cp template-config.yml.example template-config.yml

# 2. Edit configuration
# Fill in your values in template-config.yml

# 3. Run clone script with config
./scripts/clone-template.ps1 -ConfigFile template-config.yml
```

## What Gets Replaced

The clone script automatically replaces these placeholders:

- `sri-subscription` → Your project name
- `us-east-1` → Your AWS region
- `344870914438` → Your AWS account ID
- `abhee15` → Your GitHub username/org
- `` → Your custom domain (optional)
- `{{BUDGET_EMAIL}}` → Your budget alert email
- `{{COST_CENTER}}` → Your cost center

## After Cloning

1. **Navigate to new project**

   ```bash
   cd ../your-project-name
   ```

2. **Setup AWS infrastructure**

   ```bash
   ./scripts/setup.ps1
   ```

3. **Configure GitHub secrets**

   - Go to your GitHub repository
   - Add secrets listed in SETUP_INSTRUCTIONS.md

4. **Start local development**

   ```bash
   docker-compose up
   ```

5. **Deploy to AWS**
   ```bash
   ./scripts/deploy-dev.ps1
   ```

## Customization

### Adding New Services

1. Create new service directory
2. Add to docker-compose.yml
3. Add to Terraform modules
4. Update CI/CD workflows

### Changing Tech Stack

1. Update package.json (frontend)
2. Update .csproj files (backend)
3. Update Dockerfiles
4. Update CI/CD workflows

### Adding New Environments

1. Create new tfvars file
2. Update CI/CD workflows
3. Add environment-specific configs

## Troubleshooting

### Common Issues

- **Permission denied**: Make scripts executable (`chmod +x scripts/*.sh`)
- **GitHub CLI not found**: Install GitHub CLI or use manual setup
- **AWS credentials not configured**: Run `aws configure`
- **Directory already exists**: Choose different project name

### Solutions

- Check prerequisites are installed
- Verify AWS credentials
- Ensure GitHub CLI is configured
- Use unique project names

## Best Practices

1. **Use descriptive project names**
2. **Set up cost monitoring**
3. **Configure proper GitHub secrets**
4. **Test locally before deploying**
5. **Follow naming conventions**
6. **Document customizations**
7. **Keep template updated**

````

### 4. Extension Documentation

**File**: `EXTENDING.md`
```markdown
# Template Extension Guide

This guide explains how to extend the template for additional use cases.

## Adding Python Services

### For AI/ML Projects
1. **Create Python service directory**
   ```bash
   mkdir -p backend/python-service
````

2. **Add FastAPI service**

   ```python
   # backend/python-service/main.py
   from fastapi import FastAPI

   app = FastAPI()

   @app.get("/health")
   def health():
       return {"status": "healthy"}
   ```

3. **Add to docker-compose.yml**

   ```yaml
   python-service:
     build: ./backend/python-service
     ports:
       - "5004:80"
   ```

4. **Add to Terraform modules**
   ```hcl
   # terraform/deploy/modules/python-service/main.tf
   resource "aws_apprunner_service" "python_service" {
     # ... configuration
   }
   ```

### For Lambda Functions

1. **Create Lambda directory**

   ```bash
   mkdir -p backend/lambda-functions
   ```

2. **Add Lambda function**

   ```python
   # backend/lambda-functions/processor.py
   def lambda_handler(event, context):
       return {"statusCode": 200, "body": "OK"}
   ```

3. **Add to Terraform**
   ```hcl
   # terraform/deploy/modules/lambda/main.tf
   resource "aws_lambda_function" "processor" {
     # ... configuration
   }
   ```

## Adding Message Queues

### SQS Integration

1. **Add SQS to Terraform**

   ```hcl
   resource "aws_sqs_queue" "main" {
     name = "${var.project_name}-${var.environment}-queue"
   }
   ```

2. **Update services to use SQS**
   ```csharp
   // Add SQS client to services
   services.AddAWSService<IAmazonSQS>();
   ```

## Adding Vector Databases

### pgvector Extension

1. **Add to RDS module**

   ```hcl
   resource "aws_db_parameter_group" "main" {
     family = "postgres15"
     parameter {
       name  = "shared_preload_libraries"
       value = "vector"
     }
   }
   ```

2. **Add vector migrations**
   ```sql
   -- database/migrations/V5__Add_Vector_Extension.sql
   CREATE EXTENSION IF NOT EXISTS vector;
   ```

### Pinecone Integration

1. **Add Pinecone configuration**

   ```hcl
   variable "pinecone_api_key" {
     description = "Pinecone API key"
     type        = string
     sensitive   = true
   }
   ```

2. **Add to services**
   ```csharp
   services.AddSingleton<IPineconeClient>(provider =>
       new PineconeClient(provider.GetRequiredService<IConfiguration>()["Pinecone:ApiKey"]));
   ```

## Adding Background Workers

### SQS + Lambda Pattern

1. **Create Lambda function**

   ```python
   def process_message(event, context):
       # Process SQS message
       pass
   ```

2. **Add SQS trigger**
   ```hcl
   resource "aws_lambda_event_source_mapping" "sqs_trigger" {
     event_source_arn = aws_sqs_queue.main.arn
     function_name    = aws_lambda_function.processor.function_name
   }
   ```

### App Runner + SQS Pattern

1. **Add worker service**

   ```csharp
   // Background service to process SQS messages
   public class SqsWorkerService : BackgroundService
   {
       // Implementation
   }
   ```

2. **Add to docker-compose.yml**
   ```yaml
   worker-service:
     build: ./backend/WorkerService
     environment:
       - SQS_QUEUE_URL=${SQS_QUEUE_URL}
   ```

## Adding Scheduled Tasks

### EventBridge + Lambda

1. **Create scheduled Lambda**

   ```python
   def scheduled_task(event, context):
       # Run scheduled task
       pass
   ```

2. **Add EventBridge rule**
   ```hcl
   resource "aws_cloudwatch_event_rule" "schedule" {
     schedule_expression = "rate(1 hour)"
   }
   ```

## Adding Monitoring

### CloudWatch Dashboards

1. **Add dashboard module**
   ```hcl
   resource "aws_cloudwatch_dashboard" "main" {
     dashboard_name = "${var.project_name}-${var.environment}"
     dashboard_body = jsonencode({
       widgets = [
         # Dashboard configuration
       ]
     })
   }
   ```

### Custom Metrics

1. **Add CloudWatch metrics**
   ```csharp
   services.AddCloudWatchMetrics();
   ```

## Adding Caching

### Redis Integration

1. **Add ElastiCache**

   ```hcl
   resource "aws_elasticache_cluster" "redis" {
     cluster_id           = "${var.project_name}-${var.environment}-redis"
     engine               = "redis"
     node_type            = "cache.t3.micro"
     num_cache_nodes      = 1
   }
   ```

2. **Update services**
   ```csharp
   services.AddStackExchangeRedisCache(options => {
       options.Configuration = "redis-endpoint";
   });
   ```

## Best Practices

1. **Use modules for reusability**
2. **Follow naming conventions**
3. **Add proper documentation**
4. **Include tests**
5. **Update CI/CD workflows**
6. **Consider cost implications**
7. **Plan for scaling**
8. **Monitor performance**
9. **Document dependencies**
10. **Version control changes**

```

## Validation

After completing this phase, verify:

- [ ] Clone script works on Windows and Linux
- [ ] Configuration file template is complete
- [ ] All placeholders are replaced correctly
- [ ] New project structure is correct
- [ ] Git repository is initialized
- [ ] GitHub repository is created (optional)
- [ ] Setup instructions are generated
- [ ] Extension documentation is helpful
- [ ] Troubleshooting guide is comprehensive
- [ ] Template is ready for production use

## Notes

- Clone script should handle errors gracefully
- Configuration file should be well-documented
- Extension patterns should be practical
- Documentation should be comprehensive
- Template should be easy to customize
- All automation should be well-tested
- Cost implications should be considered
- Security best practices should be followed
```





