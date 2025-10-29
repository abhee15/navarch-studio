# Local Development Setup

This guide helps you run the entire NavArch Studio stack locally using Docker Compose.

## Prerequisites

✅ **Docker Desktop** installed and running
✅ **PowerShell** 7+ (Windows) or Bash (Mac/Linux)

## Quick Start

### Option 1: Using the Helper Script (Recommended)

```powershell
# Start everything (will build if needed)
.\scripts\start-local.ps1 -Build

# Or just start (if already built)
.\scripts\start-local.ps1

# Clean start (removes volumes)
.\scripts\start-local.ps1 -Clean -Build
```

### Option 2: Using Docker Compose Directly

```bash
# Build and start all services
docker-compose up --build -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

## What Gets Started

The local environment includes:

| Service | Port | Description |
|---------|------|-------------|
| **Frontend** | 3000 | React + Vite application |
| **API Gateway** | 5002 | Routes requests to backend services |
| **Identity Service** | 5001 | User authentication & JWT tokens |
| **Data Service** | 5003 | Hydrostatics & vessel data |
| **PostgreSQL** | 5433 | Database (note: different from default 5432) |
| **pgAdmin** | 5050 | Database management UI |

## Access Your Local Environment

### Main Application
🌐 **Frontend**: http://localhost:3000

- Uses **local authentication mode** (no AWS Cognito needed)
- API calls go to http://localhost:5002 (API Gateway)

### Backend Services
🔌 **API Gateway**: http://localhost:5002
- Health: http://localhost:5002/health
- Swagger: http://localhost:5002/swagger

🔐 **Identity Service**: http://localhost:5001
- Health: http://localhost:5001/health
- Swagger: http://localhost:5001/swagger

📊 **Data Service**: http://localhost:5003
- Health: http://localhost:5003/health
- Swagger: http://localhost:5003/swagger

### Database
🗄️ **pgAdmin**: http://localhost:5050
- Email: `admin@example.com`
- Password: `admin`

**Add PostgreSQL Server in pgAdmin:**
- Host: `postgres` (use service name, not localhost)
- Port: `5432` (internal port)
- Database: `sri_template_dev`
- Username: `postgres`
- Password: `postgres`

## Common Commands

### View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api-gateway
docker-compose logs -f frontend
docker-compose logs -f identity-service
docker-compose logs -f data-service
docker-compose logs -f postgres
```

### Restart a Service

```bash
# Restart single service
docker-compose restart api-gateway

# Restart all
docker-compose restart
```

### Rebuild After Code Changes

```bash
# Rebuild specific service
docker-compose build api-gateway
docker-compose up -d api-gateway

# Rebuild all
docker-compose build
docker-compose up -d
```

### Stop Everything

```bash
# Stop but keep data
docker-compose down

# Stop and remove volumes (fresh start)
docker-compose down -v
```

### Check Service Status

```bash
docker-compose ps
```

## Local vs Cloud Environment

### Key Differences

| Aspect | Local (Docker) | Cloud (AWS) |
|--------|----------------|-------------|
| **Auth** | Local JWT tokens | AWS Cognito |
| **Ports** | 3000, 5001-5003, 5433 | App Runner URLs |
| **Database** | Local PostgreSQL (5433) | AWS RDS |
| **Frontend** | Vite dev server | CloudFront + S3 |
| **Config** | Environment variables | Runtime config.json |

### Environment Variables

**Local** (set in `docker-compose.yml`):
```yaml
VITE_API_URL=http://localhost:5002
VITE_AUTH_MODE=local
```

**Cloud** (set by Terraform):
```yaml
VITE_API_URL=https://xxx.awsapprunner.com
VITE_AUTH_MODE=cognito
```

## Troubleshooting

### Services Won't Start

**Check Docker is running:**
```powershell
docker info
```

**Check ports aren't in use:**
```powershell
# Windows
netstat -ano | findstr :3000
netstat -ano | findstr :5002

# Mac/Linux
lsof -i :3000
lsof -i :5002
```

### Frontend Can't Reach Backend

**Verify API Gateway is running:**
```bash
curl http://localhost:5002/health
```

**Check frontend is using correct URL:**
- Open browser console (F12)
- Should see: `[API] Request config: { baseURL: 'http://localhost:5002/api/v1' }`

### Database Connection Issues

**Verify PostgreSQL is healthy:**
```bash
docker-compose ps postgres
# Should show: Up (healthy)
```

**Test database connection:**
```bash
docker-compose exec postgres psql -U postgres -d sri_template_dev
```

### Migrations Not Running

**Check service logs:**
```bash
docker-compose logs identity-service | grep -i migration
docker-compose logs data-service | grep -i migration
```

**Manually run migrations:**
```bash
# Identity Service
docker-compose exec identity-service dotnet ef database update

# Data Service
docker-compose exec data-service dotnet ef database update
```

### Services Take Long to Start

This is normal! Services wait for dependencies:
1. **PostgreSQL** starts first (~10 seconds)
2. **Identity & Data Services** run migrations (~20 seconds)
3. **API Gateway** waits for services to be healthy (~30 seconds)
4. **Frontend** starts last (~40 seconds)

**Total startup time: 60-90 seconds for first run**

## Development Workflow

### 1. Start Local Environment

```powershell
.\scripts\start-local.ps1 -Build
```

Wait for all services to be healthy (check with `docker-compose ps`).

### 2. Make Code Changes

Edit files in:
- `frontend/src/` - React components
- `backend/*/` - .NET services

### 3. See Changes

**Frontend** (hot reload enabled):
- Changes appear immediately in browser

**Backend** (requires rebuild):
```bash
# After changing backend code
docker-compose build data-service
docker-compose up -d data-service
```

### 4. Run Tests

```bash
# Frontend tests
cd frontend && npm test

# Backend tests
cd backend && dotnet test
```

### 5. Stop When Done

```bash
docker-compose down
```

## Testing Local Authentication

Since local mode doesn't use Cognito, you can create users directly:

### Create Test User

1. **Go to**: http://localhost:3000
2. **Click**: Sign Up
3. **Enter**: Any email/password
4. **Submit**: User is created in local PostgreSQL

### Login

1. **Go to**: http://localhost:3000/login
2. **Enter**: Email/password you created
3. **Submit**: Receives JWT token from Identity Service

## Database Development

### Access Database

**Via pgAdmin** (GUI):
1. Go to http://localhost:5050
2. Login with `admin@example.com` / `admin`
3. Add server: `postgres` / `sri_template_dev`

**Via Command Line**:
```bash
docker-compose exec postgres psql -U postgres -d sri_template_dev
```

### Useful SQL Commands

```sql
-- List all tables
\dt

-- View users
SELECT * FROM "Users";

-- View vessels
SELECT * FROM "Vessels";

-- Reset database (be careful!)
DROP SCHEMA public CASCADE;
CREATE SCHEMA public;
```

### Create Database Backup

```bash
docker-compose exec postgres pg_dump -U postgres sri_template_dev > backup.sql
```

### Restore Database Backup

```bash
cat backup.sql | docker-compose exec -T postgres psql -U postgres sri_template_dev
```

## Performance Tips

### Speed Up Rebuilds

**Use Docker layer caching:**
- Docker caches unchanged layers
- Put frequently changing code last in Dockerfile
- Dependencies are cached until `package.json` or `.csproj` changes

**Rebuild only what changed:**
```bash
# Instead of rebuilding everything
docker-compose build

# Rebuild just one service
docker-compose build api-gateway
```

### Reduce Resource Usage

**Limit Docker resources** (Docker Desktop settings):
- CPUs: 2-4 cores
- Memory: 4-8 GB
- Swap: 1 GB

**Stop unused services:**
```bash
# Only run backend (no frontend)
docker-compose up postgres identity-service data-service api-gateway

# Only run frontend (use cloud backend)
docker-compose up frontend
# Then change VITE_API_URL to cloud URL
```

## FAQ

### Q: Can I use local and cloud at the same time?

**Yes!** They're completely independent:
- Local: http://localhost:3000 → Local database
- Cloud: https://xxx.cloudfront.net → AWS RDS

### Q: Will local changes affect cloud?

**No!** Local environment is isolated. Changes only affect cloud when you:
1. Commit code
2. Push to GitHub
3. CI/CD deploys to AWS

### Q: Can I use cloud backend with local frontend?

**Yes!** Change `VITE_API_URL` in `docker-compose.yml`:
```yaml
environment:
  - VITE_API_URL=https://your-api-gateway.awsapprunner.com
  - VITE_AUTH_MODE=cognito  # Use Cognito auth
```

Then restart: `docker-compose up -d frontend`

### Q: How do I reset everything?

```bash
# Nuclear option: Remove everything
docker-compose down -v
docker system prune -a

# Then rebuild
.\scripts\start-local.ps1 -Build
```

### Q: Ports conflict with other services?

**Change ports in `docker-compose.yml`:**
```yaml
ports:
  - "8080:3000"  # Instead of 3000:3000
```

## Next Steps

- ✅ **Start local environment**: `.\scripts\start-local.ps1 -Build`
- ✅ **Access frontend**: http://localhost:3000
- ✅ **Create test user**: Sign up at http://localhost:3000
- ✅ **Test hydrostatics**: Add a vessel, run calculations
- ✅ **Check backend**: http://localhost:5002/swagger
- ✅ **View database**: http://localhost:5050

**Happy coding! 🚀**

