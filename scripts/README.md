# Development Scripts

Quick setup and management scripts for NavArch Studio development environment.

---

## 🚀 **Quick Start**

### **First Time Setup**

```bash
# Run the setup script
./scripts/dev-setup.sh

# This will:
# 1. Check prerequisites (Docker, .NET, Node.js)
# 2. Start Postgres + Redis
# 3. Apply database migrations
# 4. Seed catalog data (5K ML + 600 Real vessels)
# 5. Show service URLs
```

### **Start Development**

```bash
# Option A: Docker Compose (all services)
docker-compose up -d

# Option B: dotnet watch (better for development)
cd backend/DataService && dotnet watch run

# Start frontend
cd frontend && npm run dev
```

---

## 📜 **Available Scripts**

### **`./scripts/dev-setup.sh`** 

**Initial environment setup**

- ✅ Checks prerequisites
- ✅ Starts Postgres + Redis
- ✅ Applies fresh migrations
- ✅ Seeds catalog data
- ✅ Verifies data loaded
- ✅ Shows service URLs

**When to use:**
- First time setup
- After `dev-reset.sh`
- After pulling major database changes

**Time:** 3-5 minutes (catalog seeding)

---

### **`./scripts/dev-reset.sh`**

**Complete environment reset**

- 🛑 Stops all containers
- 🗑️ Deletes database volumes
- 🧹 Cleans build artifacts
- 🧹 Cleans node_modules

**When to use:**
- Fresh start needed
- Database corruption
- Persistent issues
- Before major updates

**⚠️ WARNING:** Deletes all data! You'll need to run `dev-setup.sh` after.

---

### **`./scripts/dev-status.sh`**

**Check environment status**

Shows:
- 🐳 Docker service status
- 🗄️ Database connection + record counts
- 🏥 Service health endpoints
- 🎨 Frontend status
- 🔨 Build status

**When to use:**
- Troubleshooting
- Verify services running
- Check data loaded
- Before starting work

---

### **`./scripts/dev-logs.sh`**

**View service logs**

Interactive menu to view logs:
1. All services
2. Postgres
3. Redis
4. IdentityService
5. DataService
6. HullSizingService
7. ApiGateway
8. PgAdmin
9. Follow all logs (live)

**When to use:**
- Debugging issues
- Monitoring startup
- Checking for errors
- Following live logs

---

## 🎯 **Common Workflows**

### **Morning Routine (Start Work)**

```bash
# Check status
./scripts/dev-status.sh

# If services not running:
docker-compose up -d

# Start frontend
cd frontend && npm run dev

# Open browser to http://localhost:5173
```

---

### **After Git Pull (Database Changes)**

```bash
# Check if migrations changed
git diff HEAD~1 backend/*/Migrations/

# If migrations changed, reapply:
cd backend/HullSizingService && dotnet ef database update
cd ../DataService && dotnet ef database update
```

---

### **Something Broke (Nuclear Option)**

```bash
# Reset everything
./scripts/dev-reset.sh

# Start fresh
./scripts/dev-setup.sh

# Start services
docker-compose up -d
cd frontend && npm run dev
```

---

### **Check Service Health**

```bash
# Quick status
./scripts/dev-status.sh

# Or manually:
curl http://localhost:5001/health  # IdentityService
curl http://localhost:5003/health  # DataService
curl http://localhost:5004/health  # HullSizingService
curl http://localhost:5002/health  # ApiGateway
```

---

### **View Logs**

```bash
# Interactive menu
./scripts/dev-logs.sh

# Or specific service:
docker-compose logs -f data-service

# Follow all:
docker-compose logs -f
```

---

## 🗄️ **Database Management**

### **Connect to Database**

```bash
# Using psql
docker exec -it navarch-studio-postgres-1 psql -U postgres -d sri_template_dev

# Or use PgAdmin
# http://localhost:5050
# Email: admin@example.com
# Password: admin
```

### **Check Catalog Data**

```sql
-- Check ML catalog
SELECT COUNT(*) FROM catalog_ml.parametric_hulls;

-- Check Real catalog
SELECT COUNT(*) FROM catalog_real.vessels;

-- Sample ML hull
SELECT hull_id, lpp_m_derived, cb_derived, dataset_source 
FROM catalog_ml.parametric_hulls 
LIMIT 5;
```

### **Manual Migration**

```bash
# HullSizingService
cd backend/HullSizingService
dotnet ef database update

# DataService
cd ../DataService
dotnet ef database update

# Drop database (if needed)
dotnet ef database drop --force
```

---

## 🐛 **Troubleshooting**

### **Script Permission Denied**

```bash
# On Linux/Mac, make scripts executable:
chmod +x scripts/*.sh

# On Windows, use Git Bash or WSL
```

---

### **Port Already in Use**

```bash
# Find process using port
lsof -i :5433  # Postgres
lsof -i :6379  # Redis

# Kill process
kill -9 <PID>

# Or change port in docker-compose.yml
```

---

### **Database Connection Failed**

```bash
# Check postgres is running
docker ps | grep postgres

# Check health
docker exec navarch-studio-postgres-1 pg_isready -U postgres

# View logs
docker-compose logs postgres

# Restart if needed
docker-compose restart postgres
```

---

### **Catalog Not Seeding**

```bash
# Check DataService logs during migration
cd backend/DataService
dotnet ef database update | tee migration.log

# Look for:
# "[SEED] Starting parametric catalog seeding..."
# "[SEED] ✅ Parametric catalog seeded successfully!"

# If failed, check CSV files exist:
ls -la .plan/app-docs/hull-sizing/data/Ship_D_Dataset/Constrained_Randomized_Set_1/
```

---

### **Redis Connection Failed**

```bash
# Check redis is running
docker ps | grep redis

# Test connection
docker exec navarch-studio-redis-1 redis-cli ping
# Expected: PONG

# Restart if needed
docker-compose restart redis
```

---

### **Frontend Won't Start**

```bash
# Reinstall dependencies
cd frontend
rm -rf node_modules package-lock.json
npm install

# Check .env.local exists
cat .env.local
# Should have VITE_API_URL=http://localhost:5002

# Try again
npm run dev
```

---

## 📊 **Service URLs (Default)**

| Service | URL | Port |
|---------|-----|------|
| **Frontend** | http://localhost:5173 | 5173 |
| **ApiGateway** | http://localhost:5002 | 5002 |
| **IdentityService** | http://localhost:5001 | 5001 |
| **DataService** | http://localhost:5003 | 5003 |
| **HullSizingService** | http://localhost:5004 | 5004 |
| **PgAdmin** | http://localhost:5050 | 5050 |
| **Postgres** | localhost:5433 | 5433 |
| **Redis** | localhost:6379 | 6379 |

---

## 🎓 **Development Tips**

### **Hot Reload (Backend)**

```bash
# Use dotnet watch instead of dotnet run
cd backend/DataService
dotnet watch run

# Changes auto-reload!
```

### **Frontend Already Has HMR**

Vite provides Hot Module Replacement out of the box. Just save and see changes instantly.

### **Run Tests**

```bash
# Backend unit tests
cd backend
dotnet test

# Frontend tests
cd frontend
npm run test
```

### **Format Code**

```bash
# Backend
cd backend
dotnet format

# Frontend
cd frontend
npm run format
```

---

## 🚀 **Ready to Code!**

**Typical dev session:**

```bash
# 1. Check status
./scripts/dev-status.sh

# 2. Start services (if not running)
docker-compose up -d

# 3. Start frontend
cd frontend && npm run dev

# 4. Open browser
# http://localhost:5173

# 5. Make changes and code!
```

**Need help?** Check `.plan/DEV-DEPLOYMENT-PLAN.md` for detailed troubleshooting.

