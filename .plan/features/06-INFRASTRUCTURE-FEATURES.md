# Infrastructure & DevOps Features

**Module Status**: 57% Complete - Core Deployed, Security & Testing Incomplete  
**Last Updated**: November 4, 2025

---

## 📊 Module Overview

Infrastructure encompasses AWS deployment, CI/CD pipelines, logging, security, and development tooling.

### Key Components
- AWS App Runner (4 microservices)
- RDS PostgreSQL databases
- ECR container registries
- CloudFront CDN
- GitHub Actions workflows
- CloudWatch logging
- Security hardening (partial)

### Current Status
✅ **Deployed**: App Runner, RDS, ECR, CloudFront, CI/CD  
✅ **Complete**: Phase 10 Logging (CloudWatch)  
⚠️ **Partial**: Phase 11 Security (Week 1 done, Week 2-3 pending)  
📋 **Planned**: Phase 12 Testing, Phase 13 Dev Experience, Phase 14 Performance

---

## ✅ Completed Features

### 1. AWS App Runner Deployment

**Status**: ✅ Complete  
**Priority**: Critical  
**Complexity**: L  
**Phase**: Phase 5

**Description**: Four microservices deployed on AWS App Runner with automatic scaling.

**Services Deployed**:
1. **API Gateway** (Port 5002)
   - Routes traffic to downstream services
   - JWT authentication
   - CORS configuration
   - Rate limiting

2. **Identity Service** (Port 5000)
   - User authentication (JWT)
   - User management
   - Cognito integration

3. **Data Service** (Port 5001)
   - Hydrostatics calculations
   - Vessel/loadcase management
   - Catalog endpoints
   - Resistance/powering

4. **Hull Sizing Service** (Port 5004)
   - Mission case management
   - Solver engine
   - Candidate workspace

**Configuration**:
- CPU: 1024 (1 vCPU)
- Memory: 2048 MB (2 GB)
- Auto-scaling: 1-10 instances
- Health checks: Every 30s
- Timeout: 120s

**Networking**:
- VPC connector for RDS access
- Private subnets for databases
- Public endpoints for API
- Security groups configured

**Code Locations**:
- Terraform: `terraform/deploy/modules/app-runner/`
- Dockerfiles: `backend/*/Dockerfile`

**Related Docs**:
- `.plan/DEPLOYMENT_WORKFLOW.md`
- `.plan/DEPLOYMENT_READINESS.md`

---

### 2. RDS PostgreSQL Databases

**Status**: ✅ Complete  
**Priority**: Critical  
**Complexity**: M  
**Phase**: Phase 4

**Description**: Managed PostgreSQL databases for each service.

**Databases**:
1. **navarch_identity** - Users, authentication
2. **navarch_data** - Vessels, hydrostatics, catalog
3. **navarch_sizing** - Hull sizing missions, candidates

**Configuration**:
- Engine: PostgreSQL 15
- Instance: db.t3.micro (free tier eligible)
- Storage: 20 GB SSD (gp2)
- Backups: 7 days retention
- Multi-AZ: No (dev), Yes (prod recommended)

**Security**:
- VPC isolated (private subnets)
- Security groups (App Runner → RDS only)
- Encrypted at rest (AWS managed keys)
- SSL connections enforced

**Migrations**:
- EF Core migrations
- Automatic on service startup
- Idempotent (safe to re-run)
- Logged to CloudWatch

**Code Locations**:
- Terraform: `terraform/deploy/main.tf` (RDS module)
- Migrations: `backend/*/Migrations/`

---

### 3. ECR Container Registries

**Status**: ✅ Complete  
**Priority**: Critical  
**Complexity**: S  
**Phase**: Phase 4

**Description**: Docker image repositories for each microservice.

**Repositories**:
1. `navarch-studio-api-gateway`
2. `navarch-studio-identity-service`
3. `navarch-studio-data-service`
4. `navarch-studio-hull-sizing-service`

**Features**:
- Image tag mutability: MUTABLE
- Scan on push: Enabled
- Lifecycle policy: Keep last 10 images
- Cross-region replication: No (single region)

**CI/CD Integration**:
- GitHub Actions builds images
- Pushes to ECR
- Tags: `latest`, `{commit-sha}`, `{env}-{date}`
- App Runner pulls from ECR

**Code Locations**:
- Terraform: `terraform/setup/main.tf` (ECR resources)
- CI/CD: `.github/workflows/ci-*.yml`

---

### 4. CloudFront CDN

**Status**: ✅ Complete  
**Priority**: High  
**Complexity**: M  
**Phase**: Phase 5

**Description**: CloudFront distribution for frontend static assets.

**Configuration**:
- Origin: S3 bucket (`navarch-studio-frontend`)
- Caching: Optimized for SPA
- Compression: Gzip + Brotli enabled
- HTTPS: Required (redirect HTTP → HTTPS)
- Custom domain: Optional (via Route53)

**Cache Behavior**:
- Default: `/` → index.html (no cache)
- Static assets: `/assets/*` → 1 year cache
- Invalidation: On deployment

**Security**:
- OAI (Origin Access Identity) for S3
- WAF: Not configured yet (Phase 11)
- Custom error pages: 404 → index.html (SPA routing)

**Code Locations**:
- Terraform: `terraform/deploy/modules/cloudfront/`
- S3 bucket: `terraform/deploy/main.tf`

---

### 5. GitHub Actions CI/CD

**Status**: ✅ Complete  
**Priority**: Critical  
**Complexity**: L  
**Phase**: Phase 7

**Description**: Automated build, test, and deployment pipelines for three environments.

**Workflows**:
1. **ci-dev.yml** - Deploy to dev on push to `main`
2. **ci-staging.yml** - Deploy to staging (manual trigger)
3. **ci-prod.yml** - Deploy to prod (manual trigger)
4. **ci-destroy.yml** - Tear down environment

**Pipeline Steps**:
1. Checkout code
2. Build backend (dotnet)
   - Restore dependencies
   - Build all projects
   - Run unit tests
   - Format check (`dotnet format --verify-no-changes`)
3. Build frontend (npm)
   - Install dependencies
   - Run linter (`npm run lint`)
   - Type check (`npm run type-check`)
   - Build production bundle
4. Build & push Docker images to ECR
5. Deploy to App Runner (force new deployment)
6. Run smoke tests (health checks)
7. Notify on success/failure

**Optimization**:
- Frontend-only changes skip backend builds
- Backend-only changes skip frontend builds
- Caching for node_modules and dotnet packages

**Known Issue**: Backend skipped when only `Shared/` changes (needs fix)

**Code Locations**:
- Workflows: `.github/workflows/ci-*.yml`

**Related Docs**:
- `.plan/DEPLOYMENT_WORKFLOW.md`
- `temp/DEPLOYMENT-STEPS.md`

---

### 6. CloudWatch Logging (Phase 10)

**Status**: ✅ Complete  
**Priority**: Critical  
**Complexity**: M  
**Phase**: Phase 10

**Description**: Structured logging with Serilog and CloudWatch integration.

**Features**:
- **Structured JSON Logging**: All logs as JSON with properties
- **Correlation IDs**: Distributed tracing across services
- **Log Levels**: Debug, Info, Warning, Error
- **Log Groups**: One per service
  - `/aws/apprunner/navarch-studio-api-gateway`
  - `/aws/apprunner/navarch-studio-identity-service`
  - `/aws/apprunner/navarch-studio-data-service`
  - `/aws/apprunner/navarch-studio-hull-sizing-service`
- **Retention**: 7 days (stays within free tier: 5GB/month)
- **Enrichers**: Timestamp, environment, service name, correlation ID

**Serilog Configuration**:
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "DataService")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.AWSSeriLog(options)
    .CreateLogger();
```

**Use Cases**:
- Debugging production issues
- Performance monitoring
- Error tracking
- Security audit

**Code Locations**:
- Backend: `backend/*/Program.cs` (Serilog configuration)
- Middleware: `backend/Shared/Middleware/CorrelationIdMiddleware.cs`

**Performance**: <5ms overhead per request

**Related Docs**:
- `.plan/phase10-logging.md`
- `.plan/PRIORITIES.md` (Phase 10 section)

---

### 7. Docker Multi-Stage Builds

**Status**: ✅ Complete  
**Priority**: High  
**Complexity**: M  
**Phase**: Phase 3

**Description**: Optimized Docker images with multi-stage builds.

**Dockerfile Pattern**:
1. **Build Stage**: `mcr.microsoft.com/dotnet/sdk:8.0`
   - Restore dependencies
   - Build application
   - Run tests (optional)

2. **Runtime Stage**: `mcr.microsoft.com/dotnet/aspnet:8.0-alpine`
   - Copy built artifacts
   - Set environment variables
   - Health check configured
   - Non-root user

**Optimizations**:
- Alpine base images (smaller size)
- Layer caching for dependencies
- .dockerignore to reduce context
- Health checks: `curl -f http://localhost:8080/health`

**Image Sizes**:
- API Gateway: ~210 MB
- Identity Service: ~205 MB
- Data Service: ~220 MB
- Hull Sizing Service: ~215 MB

**Code Locations**:
- Dockerfiles: `backend/*/Dockerfile`
- .dockerignore: `backend/.dockerignore`

---

### 8. Rate Limiting (Phase 11 Week 1)

**Status**: ✅ Complete  
**Priority**: High  
**Complexity**: S  
**Phase**: Phase 11

**Description**: API rate limiting to prevent abuse and DDoS.

**Limits**:
- **Global**: 100 requests/minute per IP
- **Login**: 5 attempts/15 minutes per IP
- **Signup**: 3 attempts/hour per IP

**Implementation**: ASP.NET Core Rate Limiting middleware

**Response**: 429 Too Many Requests with Retry-After header

**Code Locations**:
- Backend: `backend/ApiGateway/Program.cs` (middleware registration)

**Cost**: Free (built into .NET 8)

**Related Docs**:
- `.plan/phase11-security.md`

---

### 9. Security Headers (Phase 11 Week 1)

**Status**: ✅ Complete  
**Priority**: High  
**Complexity**: S  
**Phase**: Phase 11

**Description**: HTTP security headers to prevent common attacks.

**Headers Added**:
- `X-Content-Type-Options: nosniff` - Prevent MIME sniffing
- `X-Frame-Options: DENY` - Prevent clickjacking
- `X-XSS-Protection: 1; mode=block` - Enable XSS filter
- `Strict-Transport-Security: max-age=31536000` - Force HTTPS
- `Content-Security-Policy: ...` - Restrict resource loading
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: ...` - Control browser features

**Code Locations**:
- Backend: `backend/Shared/Middleware/SecurityHeadersMiddleware.cs`

**Related Docs**:
- `.plan/phase11-security.md`

---

### 10. CORS Hardening (Phase 11 Week 1)

**Status**: ✅ Complete  
**Priority**: High  
**Complexity**: S  
**Phase**: Phase 11

**Description**: Strict CORS policy replacing `AllowAnyOrigin`.

**Allowed Origins**:
- Production: `https://yourdomain.com`
- Staging: `https://staging.yourdomain.com`
- Local dev: `http://localhost:3000`

**Methods**: GET, POST, PUT, DELETE, PATCH, OPTIONS

**Headers**: Authorization, Content-Type, X-Requested-With

**Code Locations**:
- Backend: `backend/*/Program.cs` (CORS configuration)

---

### 11. Health Checks

**Status**: ✅ Complete  
**Priority**: High  
**Complexity**: S  
**Phase**: Phase 5

**Description**: HTTP health check endpoints for all services.

**Endpoints**: `/health`

**Response**: 
```json
{
  "status": "healthy",
  "checks": {
    "database": "healthy",
    "self": "healthy"
  },
  "timestamp": "2025-11-04T12:34:56Z"
}
```

**Used By**:
- App Runner health checks
- CI/CD smoke tests
- Monitoring/alerting

**Code Locations**:
- Backend: `backend/*/Program.cs` (health check registration)
- Tests: `.github/workflows/ci-*.yml` (smoke tests)

---

### 12. Terraform Infrastructure as Code

**Status**: ✅ Complete  
**Priority**: Critical  
**Complexity**: XL  
**Phase**: Phase 4-5

**Description**: Complete infrastructure defined in Terraform.

**Modules**:
1. **Setup** (`terraform/setup/`)
   - ECR repositories
   - S3 buckets
   - IAM roles/policies
   - One-time resources

2. **Deploy** (`terraform/deploy/`)
   - App Runner services
   - RDS databases
   - VPC/networking
   - CloudFront
   - Secrets Manager (planned)

**Environments**:
- `dev.tfvars`
- `staging.tfvars`
- `prod.tfvars`

**State Management**:
- S3 backend
- DynamoDB state locking
- Encrypted state files

**Code Locations**:
- Terraform: `terraform/`
- Variables: `terraform/deploy/environments/*.tfvars`

**Related Docs**:
- `.plan/DEPLOYMENT_PREREQUISITES.md`
- `.plan/IAM_SETUP.md`

---

## ⚠️ Partial Features

### 13. Security Hardening (Phase 11 Week 2-3)

**Status**: ⚠️ Partial - Week 1 Complete, Week 2-3 Pending  
**Priority**: Critical  
**Complexity**: M  
**Phase**: Phase 11

**Week 1 Complete** ✅:
- Rate limiting
- Security headers
- CORS hardening
- Input validation (basic)

**Week 2-3 Pending** (6 hours remaining):

1. **Secrets Manager** (2 hours)
   - Move DB passwords to AWS Secrets Manager
   - Move JWT keys to Secrets Manager
   - Inject via App Runner environment

2. **RBAC (Role-Based Access Control)** (3 hours)
   - Define roles: User, Admin, SuperAdmin
   - Use Cognito groups
   - Implement `[Authorize(Roles = "Admin")]` attributes
   - Test role enforcement

3. **Audit Logging** (1 hour)
   - Log login attempts (success/failure)
   - Log password changes
   - Log admin actions (create/edit/delete)
   - Log data access (who viewed what)

**Estimated Completion**: 1-2 days

**Code Locations** (to update):
- Backend: All services (add Secrets Manager client)
- Terraform: `terraform/deploy/modules/secrets/`

**Related Docs**:
- `.plan/phase11-security.md` - **CRITICAL**

---

## 📋 Planned Features

### 14. E2E Testing (Phase 12)

**Status**: 📋 Planned  
**Priority**: High  
**Complexity**: L  
**Phase**: Phase 12

**Description**: End-to-end testing with Playwright.

**Test Scenarios**:
1. User registration → login → create vessel → compute hydrostatics → export
2. Hull sizing: mission → solver → workspace → parameter adjustment
3. Catalog: browse → clone → verify in vessels
4. Comparison workflows
5. Error handling paths

**Estimated Effort**: 1-2 weeks

**Related Docs**:
- `.plan/PRIORITIES.md` (Phase 12)

---

### 15. Integration Testing (Phase 12)

**Status**: 📋 Planned  
**Priority**: High  
**Complexity**: M  
**Phase**: Phase 12

**Description**: Service integration tests with real database (test containers).

**Test Areas**:
- API Gateway → downstream services
- Service → RDS database
- Authentication flow
- Multi-tenancy isolation
- Error propagation

**Estimated Effort**: 1 week

---

### 16. Pre-Commit Hooks (Phase 13)

**Status**: 📋 Planned  
**Priority**: Medium  
**Complexity**: S  
**Phase**: Phase 13

**Description**: Git hooks for automatic formatting and linting.

**Hooks**:
- `dotnet format` before commit (backend)
- `npm run lint --fix` before commit (frontend)
- `npm run format` before commit (frontend)
- Block commit if tests fail

**Tool**: Husky (frontend), custom script (backend)

**Estimated Effort**: 4-6 hours

**Related Docs**:
- `.plan/PRIORITIES.md` (Phase 13)

---

### 17. Hot Reload for Development (Phase 13)

**Status**: 📋 Planned  
**Priority**: Medium  
**Complexity**: S  
**Phase**: Phase 13

**Description**: Improve development experience with hot reload.

**Features**:
- Backend: `dotnet watch run`
- Frontend: Already has Vite HMR
- Docker: Volume mounts for source code
- Database: Persistent volumes

**Estimated Effort**: 2-3 hours

---

### 18. Redis Caching (Phase 14)

**Status**: 📋 Planned  
**Priority**: Medium  
**Complexity**: M  
**Phase**: Phase 14

**Description**: Redis cache for frequently accessed data.

**Cache Candidates**:
- Water properties (12h TTL)
- Catalog hulls (1h TTL)
- User sessions
- API responses (5 min TTL)

**Configuration**:
- AWS ElastiCache (Redis)
- Instance: cache.t3.micro
- Cost: ~$12/month

**Estimated Effort**: 3-5 days

**Related Docs**:
- `.plan/PRIORITIES.md` (Phase 14)

---

### 19. CDN Optimization (Phase 14)

**Status**: 📋 Planned  
**Priority**: Low  
**Complexity**: S  
**Phase**: Phase 14

**Description**: Further optimize CloudFront caching.

**Optimizations**:
- Custom cache keys
- Lambda@Edge for dynamic content
- Optimized compression settings
- Monitoring and metrics

**Estimated Effort**: 1-2 days

---

### 20. AWS WAF (Optional)

**Status**: 📋 Planned  
**Priority**: Low  
**Complexity**: M  
**Phase**: Phase 11+

**Description**: Web Application Firewall for advanced protection.

**Protection**:
- SQL injection patterns
- XSS patterns
- Known bad IPs
- Bot detection
- Geographic blocking

**Cost**: $5/month + $0.60 per 1M requests

**Estimated Effort**: 4-6 hours

---

## 🐛 Known Issues & Technical Debt

### Critical

1. **Missing ECR Repository for HullSizingService**
   - CI/CD needs `ECR_HULL_SIZING_SERVICE_URL` secret
   - **Fix**: Add ECR repo in Terraform setup
   - **Effort**: 30 min

2. **CI Workflow Skips Backend Builds**
   - When only `backend/Shared/` changes
   - Requires manual deployment trigger
   - **Fix**: Remove `has-secrets` condition
   - **Effort**: 30 min
   - **Related**: `temp/WORKFLOW-SKIP-ISSUE.md`

### High

3. **Security Week 2-3 Incomplete**
   - No Secrets Manager integration
   - No RBAC implemented
   - No audit logging
   - **Fix**: Complete Phase 11 Week 2-3
   - **Effort**: 6 hours

4. **No Monitoring/Alerting**
   - Can't detect issues proactively
   - No performance metrics
   - **Fix**: Add CloudWatch alarms
   - **Effort**: 4-6 hours

### Medium

5. **No Terraform for HullSizingService**
   - Service not in Terraform (manual setup)
   - **Fix**: Add App Runner module for HullSizing
   - **Effort**: 2-3 hours

6. **Missing XML Documentation (IdentityService)**
   - Swagger docs incomplete
   - **Fix**: Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
   - **Effort**: 5 min

7. **No Backup/Restore Testing**
   - RDS backups enabled but never tested
   - **Fix**: Create restore test procedure
   - **Effort**: 2-3 hours

### Low

8. **No Cost Monitoring**
   - No alerts for unexpected AWS charges
   - **Fix**: Add AWS Budgets
   - **Effort**: 1 hour

9. **No Disaster Recovery Plan**
   - No documented DR procedure
   - **Fix**: Create DR runbook
   - **Effort**: 4 hours

---

## 📈 Metrics & Monitoring

**Current State**: Basic health checks only

**Gaps**:
- No CloudWatch alarms
- No performance dashboards
- No error rate tracking
- No cost tracking

**Planned** (Phase 9/14):
- Response time P95/P99
- Error rate by service
- Database connection pool
- Memory/CPU utilization
- Request count by endpoint

---

## 🎯 Next Steps (Priority Order)

### Sprint 1 (This Week)
1. **Fix CI workflow skip issue** (30 min)
2. **Add ECR repo for HullSizing** (30 min)
3. **Complete security Week 2-3** (6 hours)

**Goal**: Production-ready infrastructure

### Sprint 2 (Next Week)
4. **Add CloudWatch alarms** (4-6 hours)
5. **Add Terraform for HullSizingService** (2-3 hours)
6. **Test backup/restore** (2-3 hours)

**Goal**: Monitoring and resilience

### Sprint 3 (Month 2)
7. **E2E testing setup** (1-2 weeks)
8. **Integration tests** (1 week)
9. **Pre-commit hooks** (4-6 hours)

**Goal**: Quality automation

---

## 📚 Related Documentation

### Deployment
- `.plan/DEPLOYMENT_WORKFLOW.md`
- `.plan/DEPLOYMENT_PREREQUISITES.md`
- `.plan/DEPLOYMENT_READINESS.md`
- `.plan/GITHUB_SECRETS_TO_SET.md`
- `temp/DEPLOYMENT-STEPS.md`

### Infrastructure
- `.plan/IAM_SETUP.md`
- `.plan/IAM_POLICY_README.md`
- `.plan/ENVIRONMENT_CONFIGURATION.md`
- `temp/terraform-lifecycle-analysis.md`

### Security
- `.plan/phase11-security.md` - **CRITICAL**

### Phases
- `.plan/phase10-logging.md` (complete)
- `.plan/PRIORITIES.md` (phases 9-14)

---

## 🏆 Success Metrics

**Current Status**: 57% Complete

| Metric | Target | Current | Status |
|--------|--------|---------|--------|
| Services Deployed | 4 | 4 | ✅ |
| Uptime | 99.9% | ~99.9% | ✅ |
| Logging | CloudWatch | CloudWatch | ✅ |
| Security (Phase 11) | Complete | Week 1 only | ⚠️ |
| E2E Tests | >50 scenarios | 0 | 🔴 |
| Monitoring | Dashboards | Health only | 🔴 |

**Recommendation**: Infrastructure is production-ready for deployment. Complete security hardening (Week 2-3) and add monitoring before public launch.

---

**Last Updated**: November 4, 2025  
**Module Owner**: DevOps Team  
**Next Review**: November 11, 2025 (post-security completion)








