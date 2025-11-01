# NavArch Studio

Architecture management platform built with React, .NET microservices, PostgreSQL, and AWS infrastructure.

[![CI/CD](https://github.com/{username}/navarch-studio/actions/workflows/ci-dev.yml/badge.svg)](https://github.com/{username}/navarch-studio/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## ✨ Features

- **🎨 Modern Frontend**: React 18 + TypeScript + Vite + MobX + TailwindCSS
  - Professional UI with gradient backgrounds, icons, and smooth animations
  - Email verification flow with code input screen
  - Auto-navigation after login/logout
- **⚡ Microservices Backend**: .NET 8 with Identity, API Gateway, and Data services
- **📚 Reference Catalog**: Hull forms, propeller data, and water properties from ITTC and SIMMAN
  - Wigley analytical hull with complete geometry
  - ITTC water properties with temperature interpolation
  - Propeller open-water data (placeholder)
  - Clone catalog hulls to your workspace
- **🗄️ Database**: PostgreSQL 15 with EF Core migrations
- **🔐 Authentication**: AWS Cognito with JWT tokens
- **☁️ Cloud Infrastructure**: AWS (App Runner, RDS, S3, CloudFront)
- **🚀 CI/CD**: GitHub Actions with quality gates and automated deployment
  - Code quality checks run first (fail-fast approach)
  - Only deploys if all tests and linting pass
- **📦 Docker**: Full containerization with docker-compose for local development
- **🏗️ Infrastructure as Code**: Terraform for AWS resource management
- **💰 Cost-Optimized**: ~$30-50/month for dev, free tier eligible

## 🚀 Quick Start

### Prerequisites

- **Node.js** 20+
- **.NET SDK** 8.0
- **Docker Desktop**
- **AWS CLI** (for deployment)
- **Terraform** 1.5+ (for infrastructure)

### Local Development (5 minutes)

```bash
# Clone the repository
git clone https://github.com/{username}/navarch-studio.git
cd navarch-studio

# Start all services with Docker Compose
docker-compose up

# Access the application
# Frontend: http://localhost:3000
# API Gateway: http://localhost:5002/swagger
# Identity Service: http://localhost:5001/swagger
# Data Service: http://localhost:5003/swagger
# PgAdmin: http://localhost:5050 (admin@admin.com / admin)
```

### AWS Deployment (30 minutes)

See the detailed guides in `.plan/` directory:

1. **Phase 4**: [AWS Infrastructure Setup](.plan/phase4-quick-start.md)
2. **Phase 5**: [Application Deployment](.plan/phase5-quick-start.md)
3. **Phase 6**: [Authentication (Cognito)](.plan/phase6-quick-start.md)
4. **Phase 7**: [CI/CD Pipeline](.plan/phase7-quick-start.md)

## 📚 Documentation

### Getting Started

- [**Setup Guide**](docs/SETUP.md) - Prerequisites, installation, configuration
- [**Development Guide**](docs/DEVELOPMENT.md) - Local development, testing, debugging
- [**Deployment Guide**](docs/DEPLOYMENT.md) - AWS deployment, CI/CD, environments
- [**Catalog Guide**](CATALOG_IMPLEMENTATION_SUMMARY.md) - Reference data catalog overview

### Architecture & Design

- [**Architecture Overview**](docs/ARCHITECTURE.md) - System design, microservices, data flow
- [**Catalog Sources**](temp/CATALOG_SOURCES.md) - Data attribution and licensing
- [**Cost Optimization**](.plan/COST_OPTIMIZATION.md) - AWS cost breakdown and optimization strategies
- [**IAM Setup Guide**](.plan/IAM_SETUP.md) - AWS permissions and security

### CI/CD & Operations

- [**GitHub Secrets**](docs/GITHUB_SECRETS.md) - Required secrets for CI/CD
- [**Branch Protection**](docs/BRANCH_PROTECTION.md) - Git workflow and protection rules

### Phase-by-Phase Guides

- [Phase 0: Git Setup](.plan/phase0-git-setup.md)
- [Phase 1: Local Stack](.plan/phase1-local-stack.md)
- [Phase 2: Microservices](.plan/phase2-microservices.md)
- [Phase 3: Migrations & Testing](.plan/phase3-migrations-testing.md)
- [Phase 4: AWS Setup](.plan/phase4-aws-setup.md)
- [Phase 5: AWS Deployment](.plan/phase5-aws-deploy.md)
- [Phase 6: Authentication](.plan/phase6-auth.md)
- [Phase 7: CI/CD Pipeline](.plan/phase7-cicd.md)
- [Phase 8: Polish & Documentation](.plan/phase8-polish.md)

## 🏗️ Tech Stack

### Frontend

- **React** 18.3 - UI library
- **TypeScript** 5.5 - Type safety
- **Vite** 6.0 - Build tool and dev server
- **MobX** 6.15 - State management
- **TailwindCSS** 3.4 - Utility-first CSS
- **Axios** 1.12 - HTTP client
- **amazon-cognito-identity-js** 6.3 - AWS Cognito SDK

### Backend

- **.NET** 8.0 - Runtime and SDK
- **ASP.NET Core** - Web framework
- **Entity Framework Core** - ORM
- **PostgreSQL** 15 - Database
- **Npgsql** - PostgreSQL driver
- **xUnit** - Testing framework
- **Moq** - Mocking library
- **FluentAssertions** - Assertion library

### Infrastructure

- **AWS App Runner** - Container hosting
- **AWS RDS** (PostgreSQL) - Managed database
- **AWS S3** - Object storage and frontend hosting
- **AWS CloudFront** - CDN for frontend
- **AWS Cognito** - User authentication
- **AWS ECR** - Container registry
- **AWS Secrets Manager** - Secrets storage
- **AWS CloudWatch** - Logging and monitoring
- **Terraform** - Infrastructure as Code

### DevOps

- **GitHub Actions** - CI/CD pipelines
- **Docker** - Containerization
- **docker-compose** - Local orchestration
- **Flyway** - Database migrations (production)

## 📁 Project Structure

```
.
├── .github/
│   ├── workflows/          # CI/CD pipelines
│   │   ├── pr-checks.yml   # PR quality gates
│   │   ├── ci-dev.yml      # Dev deployment
│   │   ├── ci-staging.yml  # Staging deployment
│   │   └── ci-prod.yml     # Production deployment
│   └── CODEOWNERS          # Code ownership
├── .plan/                  # Phase-by-phase implementation guides
├── backend/
│   ├── IdentityService/    # User authentication service
│   ├── ApiGateway/         # API gateway and routing
│   ├── DataService/        # Business logic and data access
│   └── Shared/             # Shared models, utilities, middleware
├── frontend/
│   ├── src/
│   │   ├── components/     # React components
│   │   ├── pages/          # Page components
│   │   ├── stores/         # MobX state management
│   │   └── services/       # API clients
│   └── public/             # Static assets
├── terraform/
│   ├── setup/              # Phase 4: Infrastructure setup
│   ├── deploy/             # Phase 5: Application deployment
│   └── iam-policy.tf       # IAM policy reference
├── database/
│   ├── migrations/         # SQL migrations
│   └── seeds/              # Seed data
├── scripts/                # Automation scripts
│   ├── setup.ps1           # Infrastructure setup
│   ├── deploy.ps1          # Deployment automation
│   └── build-and-push.ps1  # Docker build and push
└── docs/                   # Documentation
```

## 🧪 Testing

### Frontend Tests

```bash
cd frontend
npm test                    # Run tests
npm run test:coverage       # Generate coverage report
npm run lint                # Lint code
npm run type-check          # TypeScript type checking
```

### Backend Tests

```bash
cd backend
dotnet test                 # Run all tests
dotnet test --logger "console;verbosity=detailed"  # Verbose output
dotnet test --collect:"XPlat Code Coverage"        # Generate coverage
```

### E2E Testing

```bash
# Start local environment
docker-compose up

# Run smoke tests
curl http://localhost:5002/health
curl http://localhost:5001/health
curl http://localhost:5003/health
```

## 🚀 Deployment

### Dev Environment

Automatically deploys on push to `develop` branch:

```bash
git checkout develop
git merge feature/your-feature
git push origin develop
# GitHub Actions automatically deploys to dev
```

### Staging Environment

Automatically deploys on push to `main` branch:

```bash
git checkout main
git merge develop
git push origin main
# GitHub Actions automatically deploys to staging
```

### Production Environment

Manual deployment with version tag:

```bash
# Go to GitHub Actions → Deploy to Production → Run workflow
# Input version tag: v1.0.0
# Requires manual approval
```

## 💰 Cost Estimates

### Development Environment

- **App Runner** (3 services): ~$15/month
- **RDS PostgreSQL** (db.t3.micro): ~$15/month
- **S3 + CloudFront**: ~$1-5/month
- **ECR**: ~$1/month
- **Cognito**: Free (< 50,000 MAU)
- **CloudWatch Logs**: ~$1-3/month

**Total: ~$30-40/month** (mostly covered by free tier in first year)

### Production Environment

- **App Runner** (3 services, scaled): ~$50-100/month
- **RDS PostgreSQL** (db.t3.medium, Multi-AZ): ~$120/month
- **S3 + CloudFront**: ~$10-20/month
- **Other services**: ~$5-10/month

**Total: ~$185-250/month**

See [Cost Optimization Guide](.plan/COST_OPTIMIZATION.md) for detailed breakdown.

## 🔒 Security

- ✅ **Authentication**: AWS Cognito with JWT tokens
- ✅ **Authorization**: Role-based access control
- ✅ **Secrets Management**: AWS Secrets Manager
- ✅ **Database**: Encrypted at rest and in transit
- ✅ **HTTPS**: Enforced for all endpoints
- ✅ **Security Scanning**: Trivy in CI/CD pipeline
- ✅ **Dependency Updates**: Dependabot enabled

## 🤝 Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Development Workflow

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'feat: add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Commit Message Convention

Follow [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `style:` Code style changes (formatting, etc.)
- `refactor:` Code refactoring
- `test:` Adding or updating tests
- `chore:` Maintenance tasks

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with modern best practices and production-ready patterns
- Optimized for AWS Free Tier eligibility
- Designed for easy customization and extension
- Comprehensive documentation for all skill levels

## 📞 Support

- **Documentation**: Browse the [docs/](docs/) directory
- **Issues**: [GitHub Issues](https://github.com/{username}/navarch-studio/issues)
- **Discussions**: [GitHub Discussions](https://github.com/{username}/navarch-studio/discussions)

## 🗺️ Roadmap

- [ ] Add monitoring dashboards (CloudWatch/Grafana)
- [ ] Add API rate limiting
- [ ] Add caching layer (Redis)
- [ ] Add WebSocket support for real-time features
- [ ] Add email service integration (SES)
- [ ] Add file upload with S3 presigned URLs
- [ ] Add comprehensive E2E testing (Playwright)
- [ ] Add load testing suite (k6)

---

**Made with ❤️ for developers who want to ship fast without compromising quality.**
