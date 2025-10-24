# Phase 0: Git & Project Foundation

## Goal
Initialize Git repository with proper structure and branching strategy to set up the foundation for the entire template project.

## Prerequisites
- Git installed (check: `git --version`)
- GitHub account (for remote repository)
- Text editor or IDE

## Deliverables Checklist

### 1. Git Repository Initialization
- [ ] Navigate to `sri-template` directory
- [ ] Run `git init`
- [ ] Create `.gitignore` file
- [ ] Create `.dockerignore` file
- [ ] Create `.gitattributes` file
- [ ] Initial commit with foundation files

### 2. .gitignore Configuration
**File**: `.gitignore`

```gitignore
# Dependencies
node_modules/
package-lock.json
yarn.lock

# .NET
bin/
obj/
*.user
*.suo
*.userosscache
*.sln.docstates
[Dd]ebug/
[Rr]elease/
x64/
x86/
[Aa]rchive/
[Bb]uild[Ll]og.*
*.DotSettings.user

# Environment variables
.env
.env.local
.env.*.local
*.env
!.env.example

# Terraform
*.tfstate
*.tfstate.*
*.tfvars
!dev.tfvars.example
!staging.tfvars.example
!prod.tfvars.example
.terraform/
.terraform.lock.hcl
terraform.tfvars

# IDE
.vscode/
.idea/
*.swp
*.swo
*~
.DS_Store

# Logs
logs/
*.log
npm-debug.log*
yarn-debug.log*
yarn-error.log*

# Docker
docker-compose.override.yml

# AWS
.aws/

# Build outputs
dist/
build/
out/
coverage/

# Secrets
secrets/
*.pem
*.key
*.cert

# Template config (should be customized per project)
template-config.yml
```

### 3. .dockerignore Configuration
**File**: `.dockerignore`

```dockerignore
# Git
.git
.gitignore
.gitattributes

# CI/CD
.github/

# Documentation
*.md
docs/

# IDE
.vscode/
.idea/
*.swp
*.swo

# Dependencies (will be installed in container)
node_modules/
bin/
obj/

# Tests
**/*.Tests/
tests/
__tests__/
*.test.ts
*.test.tsx
*.spec.ts
*.spec.tsx

# Environment files
.env
.env.*
!.env.example

# Build artifacts
dist/
build/
coverage/

# Terraform
terraform/
*.tf
*.tfvars

# Scripts
scripts/

# Docker
docker-compose*.yml
Dockerfile*
!Dockerfile
```

### 4. .gitattributes Configuration
**File**: `.gitattributes`

```gitattributes
# Auto detect text files and perform LF normalization
* text=auto

# Shell scripts should always use LF
*.sh text eol=lf
*.bash text eol=lf

# Windows scripts should always use CRLF
*.ps1 text eol=crlf
*.bat text eol=crlf
*.cmd text eol=crlf

# Source code
*.ts text eol=lf
*.tsx text eol=lf
*.js text eol=lf
*.jsx text eol=lf
*.cs text eol=lf
*.sql text eol=lf
*.json text eol=lf
*.yml text eol=lf
*.yaml text eol=lf
*.md text eol=lf
*.tf text eol=lf

# Binary files
*.png binary
*.jpg binary
*.jpeg binary
*.gif binary
*.ico binary
*.pdf binary
*.woff binary
*.woff2 binary
*.ttf binary
*.eot binary
```

### 5. Directory Structure Creation
Create all project folders:

```bash
# Root level
mkdir -p .cursor/rules
mkdir -p .plan
mkdir -p .github/workflows
mkdir -p .github/ISSUE_TEMPLATE

# Frontend
mkdir -p frontend/src/{components,stores,services,pages,types,utils,styles}
mkdir -p frontend/tests/{unit,integration}
mkdir -p frontend/public

# Backend
mkdir -p backend/Shared/{Models,DTOs,Utilities}
mkdir -p backend/IdentityService/{Controllers,Services,Models}
mkdir -p backend/IdentityService.Tests
mkdir -p backend/ApiGateway/{Middleware,Configuration}
mkdir -p backend/ApiGateway.Tests
mkdir -p backend/DataService/{Controllers,Data,Repositories,Models}
mkdir -p backend/DataService.Tests

# Database
mkdir -p database/migrations
mkdir -p database/ef-migrations
mkdir -p database/seeds

# Terraform
mkdir -p terraform/setup
mkdir -p terraform/deploy/{modules,environments}
mkdir -p terraform/deploy/modules/{app-runner,rds,s3-cloudfront,cognito,networking}

# Scripts
mkdir -p scripts

# Documentation
mkdir -p docs
```

### 6. Version Pinning Files

**File**: `global.json`
```json
{
  "sdk": {
    "version": "9.0.0",
    "rollForward": "latestMinor"
  }
}
```

**File**: `.nvmrc`
```
20.11.0
```

### 7. .editorconfig
**File**: `.editorconfig`

```ini
# EditorConfig is awesome: https://EditorConfig.org

root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true

[*.{ts,tsx,js,jsx}]
indent_style = space
indent_size = 2

[*.{cs}]
indent_style = space
indent_size = 4

[*.{json,yml,yaml}]
indent_style = space
indent_size = 2

[*.{tf,tfvars}]
indent_style = space
indent_size = 2

[*.md]
trim_trailing_whitespace = false

[*.{ps1,sh}]
indent_style = space
indent_size = 2
```

### 8. Initial README.md
**File**: `README.md`

```markdown
# sri-subscription - Full Stack Template

A production-ready template for building modern full-stack applications with React, .NET microservices, PostgreSQL, and AWS infrastructure.

## ⚠️ Template Status

**This is a template repository with folder structure only.**

The actual implementation files will be created as you follow the phase plans in order:

1. **Phase 1**: Local Development Stack - Create frontend, backend, and docker-compose
2. **Phase 2**: Microservices Split - Refactor into microservices architecture
3. **Phase 3**: Database Migrations & Testing - Add Flyway and comprehensive tests
4. **Phase 4**: AWS Infrastructure Setup - Set up AWS foundation
5. **Phase 5**: AWS App Deployment - Deploy to AWS App Runner
6. **Phase 6**: Authentication (Cognito) - Implement real authentication
7. **Phase 7**: CI/CD Pipeline - Automated deployments with GitHub Actions
8. **Phase 8**: Polish & Documentation - Complete documentation and scripts
9. **Phase 9**: Template Cloning System - Make template reusable

## Tech Stack (Planned)

### Frontend
- React 18 + TypeScript
- Vite (build tool)
- MobX (state management)
- Axios (HTTP client)
- TailwindCSS (styling)
- React Router v6 (routing)
- Jest + React Testing Library (testing)

### Backend
- .NET 9 (3 microservices)
  - Identity Service (authentication)
  - API Gateway (routing, orchestration)
  - Data Service (business logic)
- Entity Framework Core (ORM)
- xUnit (testing)
- Swagger/OpenAPI (documentation)

### Database
- PostgreSQL 15
- Flyway (migrations)

### Infrastructure
- AWS App Runner (containers)
- AWS RDS (PostgreSQL)
- AWS S3 + CloudFront (frontend hosting)
- AWS Cognito (authentication)
- AWS Secrets Manager (secrets)
- Terraform (IaC)

### CI/CD
- GitHub Actions
- Docker + ECR
- Automated deployments (dev/staging/prod)

## Getting Started

### For Template Development

1. **Complete Phase 0 (Foundation)**
   - Follow the steps in `.plan/phase0-git-setup.md`
   - Create folder structure and Git configuration
   
2. **Proceed to Phase 1**
   - See `.plan/phase1-local-stack.md`
   - Create actual implementation files
   - Set up working local development environment

3. **Continue Through Phases**
   - Follow each phase plan in order
   - Each phase builds on the previous one

### For Template Users (After Phase 9)

Once the template is complete, users can clone it using:
```bash
./scripts/clone-template.ps1  # Windows
./scripts/clone-template.sh   # Linux/Mac
```

## Current Project Structure

```
.
├── .plan/              # Phase implementation plans (complete)
├── frontend/           # React application (empty - create in Phase 1)
├── backend/            # .NET microservices (empty - create in Phase 1)
├── database/           # SQL migrations (empty - create in Phase 3)
├── terraform/          # Infrastructure as Code (empty - create in Phase 4)
├── scripts/            # Automation scripts (empty - create in Phase 8)
└── docs/               # Documentation (empty - create in Phase 8)
```

## Next Steps

After completing Phase 0, proceed to:
- **[Phase 1: Local Development Stack](.plan/phase1-local-stack.md)**

## License

MIT
```

### 9. Branch Structure Setup

```bash
# After initial commit
git branch develop
git checkout develop
```

**Branch Strategy:**
- `main` - Production-ready code, deploys to staging
- `develop` - Development branch, deploys to dev environment
- `feature/*` - Feature branches (merge to develop)
- `hotfix/*` - Urgent fixes (merge to main and develop)

### 10. GitHub Repository Setup (Manual Step)

**Instructions for user:**

1. Create new repository on GitHub (don't initialize with README)
2. Add remote:
   ```bash
   git remote add origin https://github.com/abhee15/sri-subscription.git
   ```
3. Push both branches:
   ```bash
   git push -u origin main
   git push -u origin develop
   ```
4. Set `main` as default branch
5. Configure branch protection rules:
   - `main`: Require PR reviews, require status checks
   - `develop`: Require status checks

## Validation

After completing this phase, verify:

- [ ] Git repository initialized
- [ ] All folders created
- [ ] `.gitignore`, `.dockerignore`, `.gitattributes` present
- [ ] `global.json` and `.nvmrc` created
- [ ] `.editorconfig` configured
- [ ] README.md created with placeholders
- [ ] Initial commit made
- [ ] `develop` branch created
- [ ] GitHub repository created and linked (manual)

## Next Steps

Proceed to [Phase 1: Local Development Stack](phase1-local-stack.md)

## Notes

- Placeholders like `sri-subscription` will be replaced by clone script in Phase 9
- GitHub repository creation can be automated with `gh` CLI (optional)
- Branch protection rules should be set up after first CI/CD workflow is created






