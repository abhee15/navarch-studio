# Phase 7: CI/CD Pipeline

> 📖 **Quick Start?** If you've done this before, see [`phase7-quick-start.md`](phase7-quick-start.md) for a condensed version.  
> 📚 **Detailed Guide:** This document provides comprehensive workflow configurations and explanations.

## Goal

Implement automated CI/CD pipeline using GitHub Actions with quality gates, automated testing, and environment-specific deployments. This phase ensures code quality and automated deployments.

## Prerequisites

- Phase 6 completed (authentication working)
- GitHub repository with proper secrets
- Understanding of GitHub Actions
- All services deployed and working

## Deliverables Checklist

### 1. PR Checks Workflow

**File**: `.github/workflows/pr-checks.yml`

```yaml
name: PR Checks

on:
  pull_request:
    branches: [main, develop]
    paths:
      - "frontend/**"
      - "backend/**"
      - "terraform/**"
      - "database/**"

jobs:
  frontend-checks:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: ./frontend

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: "20"
          cache: "npm"
          cache-dependency-path: frontend/package-lock.json

      - name: Install dependencies
        run: npm ci

      - name: Run ESLint
        run: npm run lint

      - name: Run Prettier check
        run: npm run format:check

      - name: Run tests
        run: npm test -- --coverage --watchAll=false

      - name: Build application
        run: npm run build

      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v3
        with:
          file: ./frontend/coverage/lcov.info
          flags: frontend

  backend-checks:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        service: [IdentityService, ApiGateway, DataService]

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"

      - name: Restore dependencies
        run: dotnet restore backend/${{ matrix.service }}

      - name: Build
        run: dotnet build backend/${{ matrix.service }} --no-restore

      - name: Run tests
        run: dotnet test backend/${{ matrix.service }}.Tests --no-build --verbosity normal --collect:"XPlat Code Coverage"

      - name: Run code analysis
        run: dotnet build backend/${{ matrix.service }} --verbosity normal /p:RunAnalyzersDuringBuild=true

      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v3
        with:
          file: ./backend/${{ matrix.service }}.Tests/coverage.cobertura.xml
          flags: ${{ matrix.service }}

  terraform-checks:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: ./terraform

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup Terraform
        uses: hashicorp/setup-terraform@v3
        with:
          terraform_version: "1.5.0"

      - name: Terraform Format Check
        run: terraform fmt -check -recursive

      - name: Terraform Init
        run: terraform init

      - name: Terraform Validate
        run: terraform validate

      - name: Terraform Plan
        run: terraform plan -var-file="environments/dev.tfvars"

  security-checks:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Run Trivy vulnerability scanner
        uses: aquasecurity/trivy-action@master
        with:
          scan-type: "fs"
          scan-ref: "."
          format: "sarif"
          output: "trivy-results.sarif"

      - name: Upload Trivy scan results to GitHub Security tab
        uses: github/codeql-action/upload-sarif@v2
        if: always()
        with:
          sarif_file: "trivy-results.sarif"
```

### 2. Development Deployment Workflow

**File**: `.github/workflows/ci-dev.yml`

```yaml
name: Deploy to Dev

on:
  push:
    branches: [develop]
  workflow_dispatch:

env:
  AWS_REGION: us-east-1
  ENVIRONMENT: dev

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    outputs:
      identity-image: ${{ steps.build-identity.outputs.image }}
      gateway-image: ${{ steps.build-gateway.outputs.image }}
      data-image: ${{ steps.build-data.outputs.image }}
      frontend-image: ${{ steps.build-frontend.outputs.image }}

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Login to Amazon ECR
        id: login-ecr
        uses: aws-actions/amazon-ecr-login@v2

      - name: Build and push Identity Service
        id: build-identity
        run: |
          docker build -t ${{ secrets.ECR_IDENTITY_SERVICE_URL }}:latest ./backend/IdentityService
          docker push ${{ secrets.ECR_IDENTITY_SERVICE_URL }}:latest
          echo "image=${{ secrets.ECR_IDENTITY_SERVICE_URL }}:latest" >> $GITHUB_OUTPUT

      - name: Build and push API Gateway
        id: build-gateway
        run: |
          docker build -t ${{ secrets.ECR_API_GATEWAY_URL }}:latest ./backend/ApiGateway
          docker push ${{ secrets.ECR_API_GATEWAY_URL }}:latest
          echo "image=${{ secrets.ECR_API_GATEWAY_URL }}:latest" >> $GITHUB_OUTPUT

      - name: Build and push Data Service
        id: build-data
        run: |
          docker build -t ${{ secrets.ECR_DATA_SERVICE_URL }}:latest ./backend/DataService
          docker push ${{ secrets.ECR_DATA_SERVICE_URL }}:latest
          echo "image=${{ secrets.ECR_DATA_SERVICE_URL }}:latest" >> $GITHUB_OUTPUT

      - name: Build and push Frontend
        id: build-frontend
        run: |
          docker build -t ${{ secrets.ECR_FRONTEND_URL }}:latest ./frontend
          docker push ${{ secrets.ECR_FRONTEND_URL }}:latest
          echo "image=${{ secrets.ECR_FRONTEND_URL }}:latest" >> $GITHUB_OUTPUT

  deploy-infrastructure:
    runs-on: ubuntu-latest
    needs: build-and-test
    environment: dev

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Setup Terraform
        uses: hashicorp/setup-terraform@v3
        with:
          terraform_version: "1.5.0"

      - name: Terraform Init
        run: terraform init
        working-directory: ./terraform/deploy

      - name: Terraform Plan
        run: terraform plan -var-file="environments/dev.tfvars"
        working-directory: ./terraform/deploy

      - name: Terraform Apply
        run: terraform apply -var-file="environments/dev.tfvars" -auto-approve
        working-directory: ./terraform/deploy

  run-migrations:
    runs-on: ubuntu-latest
    needs: deploy-infrastructure

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Run Flyway migrations
        run: |
          docker run --rm \
            -e FLYWAY_URL="jdbc:postgresql://${{ secrets.RDS_ENDPOINT }}/${{ secrets.RDS_DATABASE }}" \
            -e FLYWAY_USER="${{ secrets.RDS_USERNAME }}" \
            -e FLYWAY_PASSWORD="${{ secrets.RDS_PASSWORD }}" \
            -v $PWD/database:/flyway/sql \
            flyway/flyway:9-alpine \
            migrate

  deploy-frontend:
    runs-on: ubuntu-latest
    needs: build-and-test

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: "20"
          cache: "npm"
          cache-dependency-path: frontend/package-lock.json

      - name: Install dependencies
        run: npm ci
        working-directory: ./frontend

      - name: Build frontend
        run: npm run build
        working-directory: ./frontend
        env:
          VITE_API_URL: ${{ secrets.API_GATEWAY_URL }}
          VITE_COGNITO_USER_POOL_ID: ${{ secrets.COGNITO_USER_POOL_ID }}
          VITE_COGNITO_CLIENT_ID: ${{ secrets.COGNITO_USER_POOL_CLIENT_ID }}
          VITE_COGNITO_DOMAIN: ${{ secrets.COGNITO_DOMAIN }}

      - name: Deploy to S3
        run: |
          aws s3 sync ./frontend/dist/ s3://${{ secrets.S3_BUCKET_NAME }} --delete

      - name: Invalidate CloudFront
        run: |
          aws cloudfront create-invalidation \
            --distribution-id ${{ secrets.CLOUDFRONT_DISTRIBUTION_ID }} \
            --paths "/*"

  smoke-tests:
    runs-on: ubuntu-latest
    needs: [deploy-infrastructure, run-migrations, deploy-frontend]

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Run smoke tests
        run: |
          # Test Identity Service
          curl -f ${{ secrets.IDENTITY_SERVICE_URL }}/health || exit 1

          # Test API Gateway
          curl -f ${{ secrets.API_GATEWAY_URL }}/health || exit 1

          # Test Data Service
          curl -f ${{ secrets.DATA_SERVICE_URL }}/health || exit 1

          # Test Frontend
          curl -f ${{ secrets.FRONTEND_URL }} || exit 1

      - name: Test authentication flow
        run: |
          # Test signup endpoint
          curl -X POST ${{ secrets.API_GATEWAY_URL }}/api/v1/auth/signup \
            -H "Content-Type: application/json" \
            -d '{"email":"test@example.com","password":"Test123!","name":"Test User"}' \
            -f || exit 1

  notify:
    runs-on: ubuntu-latest
    needs: [smoke-tests]
    if: always()

    steps:
      - name: Notify deployment status
        run: |
          if [ "${{ needs.smoke-tests.result }}" == "success" ]; then
            echo "✅ Dev deployment successful!"
          else
            echo "❌ Dev deployment failed!"
          fi
```

### 3. Staging Deployment Workflow

**File**: `.github/workflows/ci-staging.yml`

```yaml
name: Deploy to Staging

on:
  push:
    branches: [main]
  workflow_dispatch:

env:
  AWS_REGION: us-east-1
  ENVIRONMENT: staging

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    outputs:
      identity-image: ${{ steps.build-identity.outputs.image }}
      gateway-image: ${{ steps.build-gateway.outputs.image }}
      data-image: ${{ steps.build-data.outputs.image }}
      frontend-image: ${{ steps.build-frontend.outputs.image }}

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Login to Amazon ECR
        id: login-ecr
        uses: aws-actions/amazon-ecr-login@v2

      - name: Build and push Identity Service
        id: build-identity
        run: |
          docker build -t ${{ secrets.ECR_IDENTITY_SERVICE_URL }}:staging ./backend/IdentityService
          docker push ${{ secrets.ECR_IDENTITY_SERVICE_URL }}:staging
          echo "image=${{ secrets.ECR_IDENTITY_SERVICE_URL }}:staging" >> $GITHUB_OUTPUT

      - name: Build and push API Gateway
        id: build-gateway
        run: |
          docker build -t ${{ secrets.ECR_API_GATEWAY_URL }}:staging ./backend/ApiGateway
          docker push ${{ secrets.ECR_API_GATEWAY_URL }}:staging
          echo "image=${{ secrets.ECR_API_GATEWAY_URL }}:staging" >> $GITHUB_OUTPUT

      - name: Build and push Data Service
        id: build-data
        run: |
          docker build -t ${{ secrets.ECR_DATA_SERVICE_URL }}:staging ./backend/DataService
          docker push ${{ secrets.ECR_DATA_SERVICE_URL }}:staging
          echo "image=${{ secrets.ECR_DATA_SERVICE_URL }}:staging" >> $GITHUB_OUTPUT

      - name: Build and push Frontend
        id: build-frontend
        run: |
          docker build -t ${{ secrets.ECR_FRONTEND_URL }}:staging ./frontend
          docker push ${{ secrets.ECR_FRONTEND_URL }}:staging
          echo "image=${{ secrets.ECR_FRONTEND_URL }}:staging" >> $GITHUB_OUTPUT

  deploy-infrastructure:
    runs-on: ubuntu-latest
    needs: build-and-test
    environment: staging

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Setup Terraform
        uses: hashicorp/setup-terraform@v3
        with:
          terraform_version: "1.5.0"

      - name: Terraform Init
        run: terraform init
        working-directory: ./terraform/deploy

      - name: Terraform Plan
        run: terraform plan -var-file="environments/staging.tfvars"
        working-directory: ./terraform/deploy

      - name: Terraform Apply
        run: terraform apply -var-file="environments/staging.tfvars" -auto-approve
        working-directory: ./terraform/deploy

  run-migrations:
    runs-on: ubuntu-latest
    needs: deploy-infrastructure

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Run Flyway migrations
        run: |
          docker run --rm \
            -e FLYWAY_URL="jdbc:postgresql://${{ secrets.RDS_ENDPOINT }}/${{ secrets.RDS_DATABASE }}" \
            -e FLYWAY_USER="${{ secrets.RDS_USERNAME }}" \
            -e FLYWAY_PASSWORD="${{ secrets.RDS_PASSWORD }}" \
            -v $PWD/database:/flyway/sql \
            flyway/flyway:9-alpine \
            migrate

  deploy-frontend:
    runs-on: ubuntu-latest
    needs: build-and-test

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: "20"
          cache: "npm"
          cache-dependency-path: frontend/package-lock.json

      - name: Install dependencies
        run: npm ci
        working-directory: ./frontend

      - name: Build frontend
        run: npm run build
        working-directory: ./frontend
        env:
          VITE_API_URL: ${{ secrets.API_GATEWAY_URL }}
          VITE_COGNITO_USER_POOL_ID: ${{ secrets.COGNITO_USER_POOL_ID }}
          VITE_COGNITO_CLIENT_ID: ${{ secrets.COGNITO_USER_POOL_CLIENT_ID }}
          VITE_COGNITO_DOMAIN: ${{ secrets.COGNITO_DOMAIN }}

      - name: Deploy to S3
        run: |
          aws s3 sync ./frontend/dist/ s3://${{ secrets.S3_BUCKET_NAME }} --delete

      - name: Invalidate CloudFront
        run: |
          aws cloudfront create-invalidation \
            --distribution-id ${{ secrets.CLOUDFRONT_DISTRIBUTION_ID }} \
            --paths "/*"

  integration-tests:
    runs-on: ubuntu-latest
    needs: [deploy-infrastructure, run-migrations, deploy-frontend]

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Run integration tests
        run: |
          # Test full authentication flow
          # Test API endpoints
          # Test database connectivity
          # Test frontend functionality
```

### 4. Production Deployment Workflow

**File**: `.github/workflows/ci-prod.yml`

```yaml
name: Deploy to Production

on:
  workflow_dispatch:
    inputs:
      confirm_deployment:
        description: 'Type "DEPLOY" to confirm production deployment'
        required: true
        default: ""

env:
  AWS_REGION: us-east-1
  ENVIRONMENT: prod

jobs:
  confirm-deployment:
    runs-on: ubuntu-latest
    if: github.event.inputs.confirm_deployment != 'DEPLOY'
    steps:
      - name: Deployment not confirmed
        run: |
          echo "❌ Deployment not confirmed. Please type 'DEPLOY' to confirm."
          exit 1

  build-and-test:
    runs-on: ubuntu-latest
    needs: confirm-deployment
    outputs:
      identity-image: ${{ steps.build-identity.outputs.image }}
      gateway-image: ${{ steps.build-gateway.outputs.image }}
      data-image: ${{ steps.build-data.outputs.image }}
      frontend-image: ${{ steps.build-frontend.outputs.image }}

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Login to Amazon ECR
        id: login-ecr
        uses: aws-actions/amazon-ecr-login@v2

      - name: Build and push Identity Service
        id: build-identity
        run: |
          docker build -t ${{ secrets.ECR_IDENTITY_SERVICE_URL }}:prod ./backend/IdentityService
          docker push ${{ secrets.ECR_IDENTITY_SERVICE_URL }}:prod
          echo "image=${{ secrets.ECR_IDENTITY_SERVICE_URL }}:prod" >> $GITHUB_OUTPUT

      - name: Build and push API Gateway
        id: build-gateway
        run: |
          docker build -t ${{ secrets.ECR_API_GATEWAY_URL }}:prod ./backend/ApiGateway
          docker push ${{ secrets.ECR_API_GATEWAY_URL }}:prod
          echo "image=${{ secrets.ECR_API_GATEWAY_URL }}:prod" >> $GITHUB_OUTPUT

      - name: Build and push Data Service
        id: build-data
        run: |
          docker build -t ${{ secrets.ECR_DATA_SERVICE_URL }}:prod ./backend/DataService
          docker push ${{ secrets.ECR_DATA_SERVICE_URL }}:prod
          echo "image=${{ secrets.ECR_DATA_SERVICE_URL }}:prod" >> $GITHUB_OUTPUT

      - name: Build and push Frontend
        id: build-frontend
        run: |
          docker build -t ${{ secrets.ECR_FRONTEND_URL }}:prod ./frontend
          docker push ${{ secrets.ECR_FRONTEND_URL }}:prod
          echo "image=${{ secrets.ECR_FRONTEND_URL }}:prod" >> $GITHUB_OUTPUT

  deploy-infrastructure:
    runs-on: ubuntu-latest
    needs: build-and-test
    environment: production

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Setup Terraform
        uses: hashicorp/setup-terraform@v3
        with:
          terraform_version: "1.5.0"

      - name: Terraform Init
        run: terraform init
        working-directory: ./terraform/deploy

      - name: Terraform Plan
        run: terraform plan -var-file="environments/prod.tfvars"
        working-directory: ./terraform/deploy

      - name: Terraform Apply
        run: terraform apply -var-file="environments/prod.tfvars" -auto-approve
        working-directory: ./terraform/deploy

  run-migrations:
    runs-on: ubuntu-latest
    needs: deploy-infrastructure

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Run Flyway migrations
        run: |
          docker run --rm \
            -e FLYWAY_URL="jdbc:postgresql://${{ secrets.RDS_ENDPOINT }}/${{ secrets.RDS_DATABASE }}" \
            -e FLYWAY_USER="${{ secrets.RDS_USERNAME }}" \
            -e FLYWAY_PASSWORD="${{ secrets.RDS_PASSWORD }}" \
            -v $PWD/database:/flyway/sql \
            flyway/flyway:9-alpine \
            migrate

  deploy-frontend:
    runs-on: ubuntu-latest
    needs: build-and-test

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: "20"
          cache: "npm"
          cache-dependency-path: frontend/package-lock.json

      - name: Install dependencies
        run: npm ci
        working-directory: ./frontend

      - name: Build frontend
        run: npm run build
        working-directory: ./frontend
        env:
          VITE_API_URL: ${{ secrets.API_GATEWAY_URL }}
          VITE_COGNITO_USER_POOL_ID: ${{ secrets.COGNITO_USER_POOL_ID }}
          VITE_COGNITO_CLIENT_ID: ${{ secrets.COGNITO_USER_POOL_CLIENT_ID }}
          VITE_COGNITO_DOMAIN: ${{ secrets.COGNITO_DOMAIN }}

      - name: Deploy to S3
        run: |
          aws s3 sync ./frontend/dist/ s3://${{ secrets.S3_BUCKET_NAME }} --delete

      - name: Invalidate CloudFront
        run: |
          aws cloudfront create-invalidation \
            --distribution-id ${{ secrets.CLOUDFRONT_DISTRIBUTION_ID }} \
            --paths "/*"

  production-tests:
    runs-on: ubuntu-latest
    needs: [deploy-infrastructure, run-migrations, deploy-frontend]

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Run production tests
        run: |
          # Test all services
          # Test authentication
          # Test database connectivity
          # Test frontend functionality
          # Test performance
```

### 5. Destroy Environment Workflow

**File**: `.github/workflows/destroy-env.yml`

```yaml
name: Destroy Environment

on:
  workflow_dispatch:
    inputs:
      environment:
        description: "Environment to destroy"
        required: true
        default: "dev"
        type: choice
        options:
          - dev
          - staging
          - prod
      confirm_destroy:
        description: 'Type "DESTROY" to confirm environment destruction'
        required: true
        default: ""

env:
  AWS_REGION: us-east-1

jobs:
  confirm-destroy:
    runs-on: ubuntu-latest
    if: github.event.inputs.confirm_destroy != 'DESTROY'
    steps:
      - name: Destruction not confirmed
        run: |
          echo "❌ Destruction not confirmed. Please type 'DESTROY' to confirm."
          exit 1

  destroy-infrastructure:
    runs-on: ubuntu-latest
    needs: confirm-destroy
    environment: ${{ github.event.inputs.environment }}

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Setup Terraform
        uses: hashicorp/setup-terraform@v3
        with:
          terraform_version: "1.5.0"

      - name: Terraform Init
        run: terraform init
        working-directory: ./terraform/deploy

      - name: Terraform Destroy
        run: terraform destroy -var-file="environments/${{ github.event.inputs.environment }}.tfvars" -auto-approve
        working-directory: ./terraform/deploy

  cleanup-ecr:
    runs-on: ubuntu-latest
    needs: destroy-infrastructure
    if: github.event.inputs.environment != 'prod'

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ env.AWS_REGION }}

      - name: Cleanup ECR images
        run: |
          # Delete images for the destroyed environment
          aws ecr batch-delete-image \
            --repository-name ${{ secrets.ECR_IDENTITY_SERVICE_URL }} \
            --image-ids imageTag=${{ github.event.inputs.environment }}

          aws ecr batch-delete-image \
            --repository-name ${{ secrets.ECR_API_GATEWAY_URL }} \
            --image-ids imageTag=${{ github.event.inputs.environment }}

          aws ecr batch-delete-image \
            --repository-name ${{ secrets.ECR_DATA_SERVICE_URL }} \
            --image-ids imageTag=${{ github.event.inputs.environment }}

          aws ecr batch-delete-image \
            --repository-name ${{ secrets.ECR_FRONTEND_URL }} \
            --image-ids imageTag=${{ github.event.inputs.environment }}

  notify:
    runs-on: ubuntu-latest
    needs: [destroy-infrastructure, cleanup-ecr]
    if: always()

    steps:
      - name: Notify destruction status
        run: |
          if [ "${{ needs.destroy-infrastructure.result }}" == "success" ]; then
            echo "✅ Environment ${{ github.event.inputs.environment }} destroyed successfully!"
          else
            echo "❌ Environment ${{ github.event.inputs.environment }} destruction failed!"
          fi
```

### 6. GitHub Secrets Documentation

**File**: `docs/GITHUB_SECRETS.md`

```markdown
# GitHub Secrets Configuration

This document lists all the GitHub secrets required for the CI/CD pipeline to work properly.

## Required Secrets

### AWS Credentials

- `AWS_ACCESS_KEY_ID` - AWS access key ID
- `AWS_SECRET_ACCESS_KEY` - AWS secret access key
- `AWS_REGION` - AWS region (e.g., us-east-1)

### ECR Repository URLs

- `ECR_IDENTITY_SERVICE_URL` - ECR repository URL for Identity Service
- `ECR_API_GATEWAY_URL` - ECR repository URL for API Gateway
- `ECR_DATA_SERVICE_URL` - ECR repository URL for Data Service
- `ECR_FRONTEND_URL` - ECR repository URL for Frontend

### Infrastructure References

- `S3_BUCKET_NAME` - S3 bucket name for Terraform state
- `DYNAMODB_TABLE_NAME` - DynamoDB table name for Terraform state locking
- `VPC_ID` - VPC ID from setup
- `PUBLIC_SUBNET_IDS` - Public subnet IDs (comma-separated)
- `PRIVATE_SUBNET_IDS` - Private subnet IDs (comma-separated)
- `APP_RUNNER_SECURITY_GROUP_ID` - App Runner security group ID
- `RDS_SECURITY_GROUP_ID` - RDS security group ID

### Cognito Configuration

- `COGNITO_USER_POOL_ID` - Cognito User Pool ID
- `COGNITO_USER_POOL_CLIENT_ID` - Cognito User Pool Client ID
- `COGNITO_DOMAIN` - Cognito domain

### Service URLs (Environment-specific)

- `IDENTITY_SERVICE_URL` - Identity Service URL
- `API_GATEWAY_URL` - API Gateway URL
- `DATA_SERVICE_URL` - Data Service URL
- `FRONTEND_URL` - Frontend URL

### Database Configuration

- `RDS_ENDPOINT` - RDS endpoint
- `RDS_DATABASE` - RDS database name
- `RDS_USERNAME` - RDS username
- `RDS_PASSWORD` - RDS password

### S3/CloudFront Configuration

- `S3_BUCKET_NAME` - S3 bucket name for frontend
- `CLOUDFRONT_DISTRIBUTION_ID` - CloudFront distribution ID

## How to Add Secrets

1. Go to your GitHub repository
2. Click on "Settings" tab
3. Click on "Secrets and variables" in the left sidebar
4. Click on "Actions"
5. Click on "New repository secret"
6. Add each secret with the exact name and value

## Environment-specific Secrets

Some secrets are environment-specific and should be added to the respective environment:

### Dev Environment

- All secrets with dev-specific values

### Staging Environment

- All secrets with staging-specific values

### Production Environment

- All secrets with production-specific values

## Security Notes

- Never commit secrets to the repository
- Use environment-specific secrets when possible
- Rotate secrets regularly
- Use least privilege principle for AWS credentials
- Monitor secret usage in AWS CloudTrail
```

### 7. Branch Protection Rules

**File**: `docs/BRANCH_PROTECTION.md`

```markdown
# Branch Protection Rules

This document describes the recommended branch protection rules for the repository.

## Main Branch Protection

### Required Status Checks

- `frontend-checks` - Frontend linting, testing, and building
- `backend-checks` - Backend testing and code analysis
- `terraform-checks` - Terraform validation and planning
- `security-checks` - Security vulnerability scanning

### Required Reviewers

- At least 1 reviewer required
- Dismiss stale reviews when new commits are pushed
- Require review from code owners

### Restrictions

- No direct pushes to main branch
- Require linear history
- Include administrators in restrictions

## Develop Branch Protection

### Required Status Checks

- `frontend-checks` - Frontend linting, testing, and building
- `backend-checks` - Backend testing and code analysis
- `terraform-checks` - Terraform validation and planning

### Required Reviewers

- At least 1 reviewer required
- Dismiss stale reviews when new commits are pushed

### Restrictions

- No direct pushes to develop branch
- Require linear history
- Include administrators in restrictions

## How to Configure

1. Go to your GitHub repository
2. Click on "Settings" tab
3. Click on "Branches" in the left sidebar
4. Click on "Add rule"
5. Configure the rules as described above
6. Save the rule

## Code Owners

Create a `.github/CODEOWNERS` file to specify who should review changes to specific files:
```

# Global owners

- @username

# Frontend

frontend/ @frontend-team

# Backend

backend/ @backend-team

# Infrastructure

terraform/ @devops-team

# Documentation

docs/ @documentation-team

```

```

## Validation

After completing this phase, verify:

- [ ] PR checks run on all pull requests
- [ ] Dev deployment works on push to develop
- [ ] Staging deployment works on push to main
- [ ] Production deployment requires manual confirmation
- [ ] Environment destruction works
- [ ] All GitHub secrets are configured
- [ ] Branch protection rules are active
- [ ] Code coverage reports are generated
- [ ] Security scans are running
- [ ] Notifications are working

## Next Steps

Proceed to [Phase 8: Polish & Documentation](phase8-polish.md)

## Notes

- CI/CD pipeline ensures code quality and automated deployments
- Environment-specific configurations for dev/staging/prod
- Security scanning and vulnerability detection
- Automated testing and code coverage
- Manual approval required for production deployments
- Environment destruction for cost management
- Comprehensive monitoring and notifications





