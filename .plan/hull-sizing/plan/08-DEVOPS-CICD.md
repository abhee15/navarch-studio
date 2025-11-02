# DevOps & CI/CD

## Docker Configuration

### docker-compose.yml Updates

Add HullSizingService to existing docker-compose:

```yaml
services:
  # ... existing services (postgres, identity-service, data-service, api-gateway, frontend)
  
  hull-sizing-service:
    build:
      context: .
      dockerfile: backend/HullSizingService/Dockerfile
    ports:
      - "5004:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=sri_template_dev;Username=postgres;Password=postgres
      - Services__DataService=http://data-service:8080
      - Jwt__SecretKey=navarch-studio-local-development-secret-key-min-32-chars
      - Jwt__Issuer=navarch-studio-local
      - Jwt__Audience=navarch-studio-api
      - FeatureFlags__DataDrivenMode=false
      - FeatureFlags__DxfExport=false
    depends_on:
      postgres:
        condition: service_healthy
      data-service:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 1024M
        reservations:
          cpus: '0.5'
          memory: 512M
```

### ApiGateway Routing Update

**backend/ApiGateway/Program.cs:**

```csharp
// Add after existing routes
app.MapWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api/v1/hull-sizing"),
    appBuilder =>
    {
        var targetUri = builder.Configuration["Services:HullSizingService"] 
            ?? "http://hull-sizing-service:8080";
        appBuilder.RunProxy(new Uri(targetUri));
    }
);
```

**backend/ApiGateway/appsettings.json:**

```json
{
  "Services": {
    "IdentityService": "http://identity-service:8080",
    "DataService": "http://data-service:8080",
    "HullSizingService": "http://hull-sizing-service:8080"
  }
}
```

---

## GitHub Actions Workflows

### Hull Sizing CI Workflow

**File:** `.github/workflows/hull-sizing-ci.yml`

```yaml
name: HullSizingService CI

on:
  pull_request:
    paths:
      - 'backend/HullSizingService/**'
      - 'backend/Shared/**'
      - '.github/workflows/hull-sizing-ci.yml'
  push:
    branches: [main, develop]
    paths:
      - 'backend/HullSizingService/**'

jobs:
  build-and-test:
    name: Build and Test
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/packages.lock.json') }}
          restore-keys: |
            ${{ runner.os }}-nuget-
      
      - name: Restore dependencies
        run: dotnet restore backend/HullSizingService/HullSizingService.csproj
      
      - name: Build
        run: dotnet build backend/HullSizingService/HullSizingService.csproj --no-restore --configuration Release
      
      - name: Run tests
        run: dotnet test backend/HullSizingService.Tests/HullSizingService.Tests.csproj --no-build --configuration Release --verbosity normal
      
      - name: Format check
        run: dotnet format backend/HullSizingService/HullSizingService.csproj --verify-no-changes
      
      - name: Security scan (Trivy)
        uses: aquasecurity/trivy-action@master
        with:
          scan-type: 'fs'
          scan-ref: 'backend/HullSizingService'
          format: 'sarif'
          output: 'trivy-results.sarif'
      
      - name: Upload Trivy results
        uses: github/codeql-action/upload-sarif@v3
        if: always()
        with:
          sarif_file: 'trivy-results.sarif'
  
  docker-build:
    name: Docker Build
    runs-on: ubuntu-latest
    needs: build-and-test
    if: github.event_name == 'push' && (github.ref == 'refs/heads/main' || github.ref == 'refs/heads/develop')
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3
      
      - name: Login to Amazon ECR
        uses: aws-actions/amazon-ecr-login@v2
        with:
          region: ${{ secrets.AWS_REGION }}
      
      - name: Build and push
        uses: docker/build-push-action@v5
        with:
          context: .
          file: backend/HullSizingService/Dockerfile
          push: true
          tags: |
            ${{ secrets.AWS_ACCOUNT_ID }}.dkr.ecr.${{ secrets.AWS_REGION }}.amazonaws.com/navarch-hull-sizing:${{ github.sha }}
            ${{ secrets.AWS_ACCOUNT_ID }}.dkr.ecr.${{ secrets.AWS_REGION }}.amazonaws.com/navarch-hull-sizing:latest
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

---

## Terraform Infrastructure

### ECR Repository

**File:** `terraform/setup/ecr.tf`

Add HullSizingService repository:

```hcl
resource "aws_ecr_repository" "hull_sizing" {
  name                 = "${var.project_name}-hull-sizing"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }

  encryption_configuration {
    encryption_type = "AES256"
  }

  tags = {
    Name        = "${var.project_name}-hull-sizing"
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}

output "hull_sizing_repository_url" {
  value = aws_ecr_repository.hull_sizing.repository_url
}
```

---

### App Runner Service

**File:** `terraform/deploy/modules/app-runner/hull-sizing.tf`

```hcl
# Data source for secrets
data "aws_secretsmanager_secret_version" "db_credentials" {
  secret_id = "navarch/rds/credentials"
}

data "aws_secretsmanager_secret_version" "jwt_secret" {
  secret_id = "navarch/jwt/secret"
}

# VPC Connector (shared with other services)
data "aws_apprunner_vpc_connector" "main" {
  arn = var.vpc_connector_arn
}

# App Runner Service
resource "aws_apprunner_service" "hull_sizing" {
  service_name = "${var.project_name}-hull-sizing-${var.environment}"

  source_configuration {
    authentication_configuration {
      access_role_arn = aws_iam_role.apprunner_ecr_access.arn
    }

    image_repository {
      image_identifier      = "${var.hull_sizing_image_uri}:${var.image_tag}"
      image_repository_type = "ECR"
      
      image_configuration {
        port = "8080"
        
        runtime_environment_variables = {
          ASPNETCORE_ENVIRONMENT = var.environment
          Services__DataService  = var.data_service_url
        }
        
        runtime_environment_secrets = {
          ConnectionStrings__DefaultConnection = data.aws_secretsmanager_secret_version.db_credentials.arn
          Jwt__SecretKey                      = data.aws_secretsmanager_secret_version.jwt_secret.arn
        }
      }
    }
    
    auto_deployments_enabled = true
  }

  instance_configuration {
    cpu    = "1024"  # 1 vCPU
    memory = "2048"  # 2 GB
  }

  health_check_configuration {
    protocol            = "HTTP"
    path                = "/health"
    interval            = 10
    timeout             = 5
    healthy_threshold   = 1
    unhealthy_threshold = 5
  }

  network_configuration {
    egress_configuration {
      egress_type       = "VPC"
      vpc_connector_arn = data.aws_apprunner_vpc_connector.main.arn
    }
  }

  observability_configuration {
    observability_enabled = true
  }

  tags = {
    Name        = "${var.project_name}-hull-sizing"
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}

# IAM Role for ECR access
resource "aws_iam_role" "apprunner_ecr_access" {
  name = "${var.project_name}-hull-sizing-apprunner-ecr-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "build.apprunner.amazonaws.com"
        }
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "apprunner_ecr_access" {
  role       = aws_iam_role.apprunner_ecr_access.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSAppRunnerServicePolicyForECRAccess"
}

# CloudWatch Log Group
resource "aws_cloudwatch_log_group" "hull_sizing" {
  name              = "/aws/apprunner/${aws_apprunner_service.hull_sizing.service_name}"
  retention_in_days = 7

  tags = {
    Environment = var.environment
    Service     = "hull-sizing"
  }
}

# Outputs
output "hull_sizing_service_url" {
  value = aws_apprunner_service.hull_sizing.service_url
}

output "hull_sizing_service_arn" {
  value = aws_apprunner_service.hull_sizing.arn
}
```

---

### Variables

**File:** `terraform/deploy/variables.tf`

Add:
```hcl
variable "hull_sizing_image_uri" {
  description = "ECR repository URI for hull-sizing service"
  type        = string
}
```

**File:** `terraform/deploy/terraform.tfvars.example`

Add:
```hcl
hull_sizing_image_uri = "123456789012.dkr.ecr.us-east-1.amazonaws.com/navarch-hull-sizing"
```

---

## Deployment Workflow

### GitHub Actions Deploy Workflow

**File:** `.github/workflows/deploy.yml`

Add job for hull-sizing:

```yaml
jobs:
  # ... existing jobs (identity, data, gateway)
  
  deploy-hull-sizing:
    name: Deploy HullSizingService
    runs-on: ubuntu-latest
    needs: [build-hull-sizing]
    if: github.ref == 'refs/heads/main' || github.ref == 'refs/heads/develop'
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v4
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ secrets.AWS_REGION }}
      
      - name: Terraform Init
        working-directory: terraform/deploy
        run: terraform init
      
      - name: Terraform Plan
        working-directory: terraform/deploy
        run: terraform plan -var="hull_sizing_image_uri=${{ secrets.AWS_ACCOUNT_ID }}.dkr.ecr.${{ secrets.AWS_REGION }}.amazonaws.com/navarch-hull-sizing" -var="image_tag=${{ github.sha }}" -out=tfplan
      
      - name: Terraform Apply
        working-directory: terraform/deploy
        run: terraform apply tfplan
```

---

## Monitoring & Observability

### CloudWatch Metrics

**Custom Metrics (from code):**

```csharp
// In FirstPrinciplesSolver.cs
using var activity = Activity.StartActivity("SizingRun");
activity?.SetTag("mission.type", mission.MissionType);
activity?.SetTag("cargo.basis", mission.CargoBasis);

var stopwatch = Stopwatch.StartNew();
var candidates = await GenerateCandidatesAsync(...);
stopwatch.Stop();

activity?.SetTag("compute.time_ms", stopwatch.ElapsedMilliseconds);
activity?.SetTag("candidates.count", candidates.Count);

// Publish custom metric
await _cloudWatch.PutMetricDataAsync(new PutMetricDataRequest
{
    Namespace = "NavArch/HullSizing",
    MetricData = new List<MetricDatum>
    {
        new MetricDatum
        {
            MetricName = "SizingRunDuration",
            Value = stopwatch.ElapsedMilliseconds,
            Unit = StandardUnit.Milliseconds,
            Timestamp = DateTime.UtcNow
        }
    }
});
```

**Metrics to Track:**
- `SizingRunDuration` (ms) - histogram
- `DisplacementClosureIterations` - average
- `DisplacementClosureError` (%) - histogram
- `CandidatesGenerated` - count
- `PushToHydrostaticsSuccess` - count
- `PushToHydrostaticsFailure` - count
- `CacheHitRate` (water properties) - percentage

---

### CloudWatch Alarms

```hcl
resource "aws_cloudwatch_metric_alarm" "hull_sizing_high_error_rate" {
  alarm_name          = "${var.project_name}-hull-sizing-high-error-rate-${var.environment}"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "5XXError"
  namespace           = "AWS/AppRunner"
  period              = 60
  statistic           = "Sum"
  threshold           = 10
  alarm_description   = "HullSizingService error rate is high"
  alarm_actions       = [var.sns_topic_arn]

  dimensions = {
    ServiceName = aws_apprunner_service.hull_sizing.service_name
  }
}

resource "aws_cloudwatch_metric_alarm" "hull_sizing_high_latency" {
  alarm_name          = "${var.project_name}-hull-sizing-high-latency-${var.environment}"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "RequestLatency"
  namespace           = "AWS/AppRunner"
  period              = 60
  statistic           = "Average"
  threshold           = 2000 # 2 seconds
  alarm_description   = "HullSizingService latency is high"
  alarm_actions       = [var.sns_topic_arn]

  dimensions = {
    ServiceName = aws_apprunner_service.hull_sizing.service_name
  }
}
```

---

### OpenTelemetry Tracing (AWS X-Ray)

**Configuration in Program.cs:**

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("HullSizingService"))
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddSource("HullSizingService");

        // Add AWS X-Ray exporter in production
        if (!builder.Environment.IsDevelopment())
        {
            tracerProviderBuilder.AddXRayTraceId(); // Propagate X-Ray trace ID
            tracerProviderBuilder.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:2000"); // AWS X-Ray daemon
            });
        }
        else
        {
            tracerProviderBuilder.AddConsoleExporter();
        }
    });
```

**Example Trace:**
```
Trace ID: 1-5f8a1234-5678abcd9012efgh3456ijkl

├─ ApiGateway: POST /api/v1/hull-sizing/runs [1850ms]
│  └─ HullSizingService: POST /runs [1820ms]
│     ├─ HullSizingService: FirstPrinciplesSolver.SolveAsync [1780ms]
│     │  ├─ HullSizingService: HTTP GET /catalog/water-properties [45ms]
│     │  │  └─ DataService: GET /catalog/water-properties [35ms]
│     │  │     └─ PostgreSQL: SELECT * FROM data.catalog_water_properties [5ms]
│     │  ├─ HullSizingService: DisplacementClosure (container) [320ms]
│     │  ├─ HullSizingService: DisplacementClosure (tanker) [310ms]
│     │  ├─ HullSizingService: DisplacementClosure (bulk) [290ms]
│     │  ├─ HullSizingService: HoltropResistance (container) [85ms]
│     │  ├─ HullSizingService: HoltropResistance (tanker) [82ms]
│     │  └─ HullSizingService: HoltropResistance (bulk) [78ms]
│     └─ PostgreSQL: INSERT INTO sizing.candidate_designs [25ms]
```

---

## Secrets Management

### AWS Secrets Manager

**Secrets:**
```
navarch/rds/credentials
  - host, port, database, username, password

navarch/jwt/secret
  - secret_key, issuer, audience

navarch/cors/origins
  - allowed_origins (comma-separated)
```

**Terraform:**
```hcl
data "aws_secretsmanager_secret_version" "db_credentials" {
  secret_id = "navarch/rds/credentials"
}

data "aws_secretsmanager_secret_version" "jwt_secret" {
  secret_id = "navarch/jwt/secret"
}

# Inject into App Runner
resource "aws_apprunner_service" "hull_sizing" {
  # ...
  
  source_configuration {
    image_repository {
      image_configuration {
        runtime_environment_secrets = {
          ConnectionStrings__DefaultConnection = data.aws_secretsmanager_secret_version.db_credentials.arn
          Jwt__SecretKey                      = data.aws_secretsmanager_secret_version.jwt_secret.arn
        }
      }
    }
  }
}
```

---

## Database Migrations in Production

### Strategy: Auto-migrate on startup (non-Development)

**Program.cs:**
```csharp
if (app.Environment.EnvironmentName != "Development")
{
    Console.WriteLine("[MIGRATION] Auto-applying pending migrations in {0}", app.Environment.EnvironmentName);
    await dbContext.Database.MigrateAsync();
}
```

**Alternative: Manual migration via GitHub Actions**

```yaml
- name: Run migrations
  run: |
    dotnet ef database update --project backend/HullSizingService --context SizingDbContext
  env:
    ConnectionStrings__DefaultConnection: ${{ secrets.DB_CONNECTION_STRING }}
```

---

## Local Development Setup

### Prerequisites
```bash
# Check prerequisites
docker --version
docker-compose --version
dotnet --version  # Should be 8.0.x
```

### First-Time Setup

```bash
# 1. Clone repository
git clone https://github.com/your-org/navarch-studio.git
cd navarch-studio

# 2. Start all services
docker-compose up --build

# Services will be available at:
# - Frontend: http://localhost:3000
# - ApiGateway: http://localhost:5002
# - IdentityService: http://localhost:5001
# - DataService: http://localhost:5003
# - HullSizingService: http://localhost:5004
# - PostgreSQL: localhost:5433

# 3. Apply migrations (if not auto-applied)
dotnet ef database update --project backend/HullSizingService --context SizingDbContext

# 4. Verify health
curl http://localhost:5004/health
```

### Running Individual Service

```bash
# Run HullSizingService only
cd backend/HullSizingService
dotnet run

# Access Swagger UI
open http://localhost:5004/swagger
```

---

## Deployment Checklist

### Pre-Deployment

- [ ] All unit tests pass
- [ ] Integration tests pass
- [ ] E2E tests pass (Cypress)
- [ ] Format check passes (`dotnet format --verify-no-changes`)
- [ ] Trivy security scan clean (no critical vulnerabilities)
- [ ] Swagger UI accessible
- [ ] Health endpoint responds

### Infrastructure

- [ ] ECR repository created (`navarch-hull-sizing`)
- [ ] App Runner service configured
- [ ] VPC connector attached (for RDS access)
- [ ] Secrets in AWS Secrets Manager
- [ ] CloudWatch alarms configured
- [ ] IAM roles/policies attached

### Post-Deployment Validation

- [ ] Service accessible via API Gateway
- [ ] Health check returns 200 OK
- [ ] Create mission case succeeds
- [ ] Run sizing succeeds (generates candidates)
- [ ] Push to Hydrostatics succeeds (creates vessel in DataService)
- [ ] CloudWatch metrics appearing
- [ ] OpenTelemetry traces in X-Ray
- [ ] No errors in CloudWatch logs

---

## Rollback Strategy

### If Deployment Fails

```bash
# 1. Revert Terraform changes
cd terraform/deploy
terraform apply -var="hull_sizing_image_uri=<previous_image_tag>"

# 2. Revert database migration (if applied)
dotnet ef migrations remove --project backend/HullSizingService --context SizingDbContext

# 3. Monitor logs
aws logs tail /aws/apprunner/navarch-hull-sizing-production --follow
```

### Feature Flags for Gradual Rollout

```json
{
  "FeatureFlags": {
    "HullSizingEnabled": true,  // Master kill switch
    "DataDrivenMode": false,    // Phase 2 feature
    "DxfExport": false,         // Phase 2 feature
    "PlaningMode": false        // Phase 3 feature
  }
}
```

Check in controllers:
```csharp
if (!_configuration.GetValue<bool>("FeatureFlags:DataDrivenMode"))
{
    return BadRequest("Data-driven mode not enabled");
}
```

---

## Next: Read `09-PERFORMANCE-TARGETS.md` for optimization strategies
