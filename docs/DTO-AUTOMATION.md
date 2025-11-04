# TypeScript DTO Automation

## Overview

This project uses **NSwag** to automatically generate TypeScript types from C# DTOs, ensuring perfect sync between frontend and backend without manual copying.

---

## 🎯 Why Automate DTOs?

### Problems We Solved:
- ❌ **Manual sync errors** - Forgetting to update TypeScript when C# changes
- ❌ **Property name mismatches** - `beamM` vs `bM`, `draftM` vs `tM`
- ❌ **Missing properties** - Easy to forget new fields
- ❌ **Runtime errors** - Type mismatches discovered too late

### Benefits:
- ✅ **Zero manual sync** - Types auto-generate from backend
- ✅ **Compile-time safety** - TypeScript catches contract changes
- ✅ **Perfect accuracy** - Generated types always match backend
- ✅ **CI/CD validation** - PRs fail if types drift
- ✅ **Better DX** - Autocomplete, inline docs, refactoring support

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     DEVELOPMENT FLOW                         │
└─────────────────────────────────────────────────────────────┘

1. UPDATE C# DTOs
   backend/Shared/DTOs/VesselDto.cs
   ↓

2. RUN BACKEND SERVICE  
   dotnet run (in backend/DataService)
   ↓

3. EXPORT SWAGGER.JSON
   pwsh scripts/export-swagger.ps1
   Downloads: http://localhost:5000/swagger/v1/swagger.json
   Saves to: backend/DataService/swagger.json
   ↓

4. GENERATE TS TYPES
   pwsh scripts/generate-types.ps1 -Mode ci
   Reads: backend/*/swagger.json
   Generates: frontend/src/api/generated/*.types.ts
   ↓

5. COMMIT FILES
   git add backend/*/swagger.json
   git add frontend/src/api/generated/*.types.ts
   git commit -m "feat: update vessel DTOs"
   ↓

6. PUSH TO GITHUB
   git push origin feature/update-vessel-dtos
   ↓

7. CI/CD VALIDATES
   GitHub Actions:
   - Regenerates types from swagger.json
   - Compares with committed types
   - Fails PR if mismatch
   ✓ Ensures sync

┌─────────────────────────────────────────────────────────────┐
│                       CI/CD FLOW                             │
└─────────────────────────────────────────────────────────────┘

PR Created
↓
GitHub Actions (.github/workflows/validate-types.yml)
├─ Install NSwag
├─ Generate types from swagger.json (CI mode)
├─ Compare with committed files
├─ Run TypeScript type-check
└─ Pass/Fail PR based on sync status
```

---

## 📁 File Organization

```
navarch-studio/
├── backend/
│   ├── Shared/
│   │   └── DTOs/                   # ⚙️  Source of truth (C# DTOs)
│   │       ├── VesselDto.cs
│   │       ├── LoadcaseDto.cs
│   │       └── ...
│   ├── DataService/
│   │   ├── swagger.json            # 📄 Exported OpenAPI spec (committed)
│   │   └── Program.cs              # Swagger UI enabled
│   ├── HullSizingService/
│   │   └── swagger.json            # 📄 Exported OpenAPI spec (committed)
│   └── IdentityService/
│       └── swagger.json            # 📄 Exported OpenAPI spec (committed)
│
├── frontend/
│   ├── src/
│   │   ├── api/
│   │   │   └── generated/          # 🤖 Auto-generated (committed)
│   │   │       ├── hydrostatics.types.ts
│   │   │       ├── sizing.types.ts
│   │   │       ├── identity.types.ts
│   │   │       ├── index.ts        # Re-exports all types
│   │   │       └── README.md
│   │   └── types/                  # 📝 Custom types (not from backend)
│   │       ├── workspace.ts
│   │       └── errors.ts
│   └── package.json                # NPM scripts for type generation
│
├── scripts/
│   ├── export-swagger.ps1          # 📤 Export swagger.json from services
│   └── generate-types.ps1          # 🔧 Generate TS types from swagger.json
│
└── .github/
    └── workflows/
        └── validate-types.yml      # ✅ CI/CD validation workflow
```

---

## 🚀 Quick Start

### For Developers (Local)

#### 1. When Backend DTOs Change

```bash
# Step 1: Start backend service(s)
cd backend/DataService
dotnet run

# In another terminal...

# Step 2: Export swagger.json
cd navarch-studio
pwsh scripts/export-swagger.ps1

# Step 3: Generate TypeScript types
pwsh scripts/generate-types.ps1 -Mode ci

# Step 4: Commit changes
git add backend/*/swagger.json
git add frontend/src/api/generated/*.types.ts
git commit -m "feat: update vessel DTOs"
git push
```

#### 2. Quick Generate (Service Running)

```bash
# If service is already running, generate directly
npm run generate:types --prefix frontend

# This will:
# - Download swagger.json from http://localhost:5000/swagger/v1/swagger.json
# - Generate TypeScript types
# - Update frontend/src/api/generated/*.types.ts
```

#### 3. Using NPM Scripts

```bash
cd frontend

# Generate from running services (local development)
npm run generate:types

# Generate from committed swagger.json files (CI mode)
npm run generate:types:ci

# Export swagger.json from services
npm run export:swagger
```

---

## 🔧 Scripts

### `export-swagger.ps1`

**Purpose:** Download Swagger/OpenAPI spec from running services

**Usage:**
```powershell
pwsh scripts/export-swagger.ps1
```

**What it does:**
1. Checks if services are running (ports 5000, 5001, 5003)
2. Downloads swagger.json from each service
3. Saves to `backend/{ServiceName}/swagger.json`
4. Adds metadata (generation date, service name)

**Requirements:**
- Backend services must be running
- Swagger UI must be enabled (it is by default)

**Output:**
```
backend/DataService/swagger.json
backend/HullSizingService/swagger.json
backend/IdentityService/swagger.json
```

---

### `generate-types.ps1`

**Purpose:** Generate TypeScript interfaces from Swagger specs

**Usage:**
```powershell
# Local mode (uses running services)
pwsh scripts/generate-types.ps1 -Mode local

# CI mode (uses committed swagger.json files)
pwsh scripts/generate-types.ps1 -Mode ci
```

**Modes:**

**Local Mode** (default)
- Tries to connect to running services
- Falls back to swagger.json files if service offline
- Best for active development

**CI Mode**
- Always uses committed swagger.json files
- Doesn't need services running
- Best for GitHub Actions / CI/CD

**What it does:**
1. Installs NSwag if missing
2. Reads Swagger spec (from URL or file)
3. Generates TypeScript interfaces
4. Adds header comments with metadata
5. Creates `index.ts` for re-exports
6. Creates `README.md` with documentation

**Output:**
```
frontend/src/api/generated/hydrostatics.types.ts
frontend/src/api/generated/sizing.types.ts
frontend/src/api/generated/identity.types.ts
frontend/src/api/generated/index.ts
frontend/src/api/generated/README.md
```

---

## 🔄 GitHub Actions Integration

### Workflow: `validate-types.yml`

**Triggers:**
- Pull requests to `main` or `develop`
- Pushes to `main`
- Changes to:
  - `backend/Shared/DTOs/**`
  - `backend/*/swagger.json`
  - `frontend/src/api/generated/**`

**Jobs:**

#### 1. `validate-types`
**Purpose:** Ensure frontend types match backend DTOs

**Steps:**
1. Checkout code
2. Setup .NET & Node.js
3. Install NSwag
4. Generate types from swagger.json (CI mode)
5. Compare generated vs committed files
6. Fail if mismatch detected

**Why it matters:**
- Prevents merging code with out-of-sync types
- Forces developers to regenerate types after DTO changes
- Guarantees frontend always matches backend

#### 2. `type-check`
**Purpose:** Validate TypeScript compiles without errors

**Steps:**
1. Install frontend dependencies
2. Run `tsc --noEmit` (type check only)
3. Run ESLint

**Why it matters:**
- Catches type errors early
- Ensures generated types work with existing code
- Validates imports/exports

---

## 💡 Common Workflows

### Scenario 1: Add New Property to DTO

```bash
# 1. Edit C# DTO
# File: backend/Shared/DTOs/VesselDto.cs
public record VesselDto
{
    // ... existing properties
    
    [Convertible("Length")]
    public decimal FreeboardM { get; init; }  // ← NEW
}

# 2. Start service
cd backend/DataService
dotnet run

# 3. Export swagger
pwsh scripts/export-swagger.ps1

# 4. Generate types
pwsh scripts/generate-types.ps1 -Mode ci

# 5. Use in frontend
import { VesselDto } from '@/api/generated';

const vessel: VesselDto = {
    // ... existing properties
    freeboardM: 2.5,  // ← TypeScript now knows about this!
};

# 6. Commit everything
git add backend/Shared/DTOs/VesselDto.cs
git add backend/DataService/swagger.json
git add frontend/src/api/generated/hydrostatics.types.ts
git commit -m "feat: add freeboard to vessel DTO"
```

---

### Scenario 2: Rename DTO Property

```bash
# 1. Rename in C# (with refactoring tool)
# Old: public decimal BeamM { get; init; }
# New: public decimal WidthM { get; init; }

# 2. Export & generate
pwsh scripts/export-swagger.ps1
pwsh scripts/generate-types.ps1 -Mode ci

# 3. Frontend code now has compile errors!
# OLD: vessel.beamM  ← TypeScript error: Property doesn't exist
# NEW: vessel.widthM ← Use this instead

# 4. Fix all TypeScript errors (compiler helps!)

# 5. Commit
git add -A
git commit -m "refactor: rename BeamM to WidthM"
```

---

### Scenario 3: Working Offline / Services Down

```bash
# If services aren't running, use CI mode
pwsh scripts/generate-types.ps1 -Mode ci

# This uses the last exported swagger.json files
# Good for:
# - Working on a plane
# - CI/CD environments
# - When services won't start
```

---

### Scenario 4: Fresh Clone / New Developer

```bash
# 1. Clone repo
git clone https://github.com/your-org/navarch-studio.git
cd navarch-studio

# 2. Install global tools
dotnet tool install -g NSwag.ConsoleCore

# 3. Generate types from committed swagger.json
cd frontend
npm run generate:types:ci

# 4. Start developing!
npm run dev

# Note: Types are already committed, so this step is optional
# But it's good to verify generation works
```

---

## 🐛 Troubleshooting

### Error: "NSwag not found"

**Solution:**
```bash
dotnet tool install -g NSwag.ConsoleCore --no-cache
```

---

### Error: "Service not running on port 5000"

**Solutions:**

**Option 1:** Start the service
```bash
cd backend/DataService
dotnet run
```

**Option 2:** Use CI mode (swagger.json files)
```bash
pwsh scripts/generate-types.ps1 -Mode ci
```

---

### Error: "Swagger file not found"

**Cause:** swagger.json hasn't been exported yet

**Solution:**
```bash
# Start service first
cd backend/DataService
dotnet run

# Then export
pwsh scripts/export-swagger.ps1
```

---

### Error: "Types are out of sync" (in PR)

**Cause:** Backend DTOs changed but types weren't regenerated

**Solution:**
```bash
# Regenerate types
pwsh scripts/export-swagger.ps1
pwsh scripts/generate-types.ps1 -Mode ci

# Commit updated files
git add backend/*/swagger.json
git add frontend/src/api/generated/*.types.ts
git commit --amend --no-edit
git push --force-with-lease
```

---

### Generated types have incorrect property names

**Cause:** NSwag configuration might need adjustment

**Solution:**
Edit `scripts/generate-types.ps1`:
```powershell
# In the $config object:
codeGenerators = @{
    openApiToTypeScriptClient = @{
        # ... other settings
        propertyNameGeneratorType = "CamelCasePropertyNameGenerator"  # Try this
    }
}
```

---

## 📊 Type Mapping

| C# Type | TypeScript Type | Notes |
|---------|-----------------|-------|
| `string` | `string` | Direct mapping |
| `int`, `long`, `decimal` | `number` | All numbers in TS |
| `bool` | `boolean` | Direct mapping |
| `DateTime` | `string` | ISO 8601 format |
| `Guid` | `string` | UUID format |
| `decimal?` | `number \| undefined` | Nullable becomes optional |
| `List<T>` | `T[]` | Array type |
| `Dictionary<string, T>` | `{ [key: string]: T }` | Index signature |
| `enum` | `enum` | TS enum generated |

---

## 🔒 Best Practices

### 1. Always Commit Generated Files
```bash
# ✅ DO commit these
git add frontend/src/api/generated/*.types.ts
git add backend/*/swagger.json

# ❌ DON'T gitignore generated files
# They ensure consistency even if backend is down
```

### 2. Regenerate After Every DTO Change
```bash
# Always run both scripts
pwsh scripts/export-swagger.ps1
pwsh scripts/generate-types.ps1 -Mode ci
```

### 3. Use Re-exports for Convenience
```typescript
// In frontend/src/types/index.ts
export type { VesselDto as Vessel } from '@/api/generated';
export type { LoadcaseDto as Loadcase } from '@/api/generated';

// Usage
import { Vessel, Loadcase } from '@/types';  // Cleaner!
```

### 4. Keep Custom Types Separate
```
frontend/src/
├── api/generated/    ← Auto-generated (from backend)
└── types/            ← Custom (frontend-only)
    ├── workspace.ts
    └── errors.ts
```

### 5. Review Generated Types in PRs
- Check diffs in `frontend/src/api/generated/*.types.ts`
- Verify property names match expectations
- Ensure nullable types are correct

---

## 🎓 Learning Resources

- [NSwag Documentation](https://github.com/RicoSuter/NSwag)
- [OpenAPI Specification](https://swagger.io/specification/)
- [TypeScript Type System](https://www.typescriptlang.org/docs/handbook/2/types-from-types.html)

---

## 🚀 Future Enhancements

### Planned:
- [ ] Auto-generate API client functions (not just types)
- [ ] Validation decorators from C# attributes
- [ ] OpenAPI 3.1 support
- [ ] Multi-version API type generation

### Ideas:
- Swagger file caching in CI (skip if unchanged)
- Pre-commit hook to regenerate types
- VSCode extension for one-click generation
- Type generation dashboard

---

## 📞 Support

**Issues with type generation?**
1. Check this documentation
2. Review error messages carefully
3. Ask in #engineering channel
4. Open GitHub issue with `[dto-automation]` tag

**Questions?**
- How do I add a new service? → Add to arrays in both scripts
- How do I change type mappings? → Edit NSwag config in `generate-types.ps1`
- How do I skip a service? → Comment it out in scripts
- Can I run this on Windows/Mac/Linux? → Yes, PowerShell Core is cross-platform

---

**Last Updated:** 2025-01-04
**Maintained By:** Engineering Team

