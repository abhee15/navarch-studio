# TypeScript DTO Automation Guide

## Overview

Automated TypeScript type generation from C# DTOs using **NSwag** and **Swagger/OpenAPI**.

**Zero manual synchronization. Perfect type safety. CI/CD validated.**

---

## 🎯 Quick Start

### For Developers

```bash
# 1. Export swagger from running services
pwsh scripts/export-swagger.ps1

# 2. Generate TypeScript types
pwsh scripts/generate-types.ps1 -Mode ci

# 3. Use in frontend
import { VesselDto, LoadcaseDto } from '@/api/generated';
```

### For CI/CD

```bash
# GitHub Actions automatically:
# - Generates types from committed swagger.json
# - Validates types match committed files
# - Fails PR if out of sync
```

---

## 📁 Architecture

```
Backend (C# DTOs)
    ↓
Swagger/OpenAPI Spec (swagger.json)
    ↓
NSwag (Code Generator)
    ↓
TypeScript Interfaces (.types.ts)
    ↓
Frontend Code (Perfect Type Safety)
```

---

## 🚀 Scripts

### `export-swagger.ps1`

Downloads swagger.json from running services.

**Usage:**
```powershell
pwsh scripts/export-swagger.ps1
```

**Requirements:**
- Backend services running (ports 5000, 5001, 5003)

**Output:**
```
backend/DataService/swagger.json
backend/HullSizingService/swagger.json
backend/IdentityService/swagger.json
```

---

### `generate-types.ps1`

Generates TypeScript from swagger.json.

**Usage:**
```powershell
# Local mode (prefers running services)
pwsh scripts/generate-types.ps1 -Mode local

# CI mode (uses swagger.json files only)
pwsh scripts/generate-types.ps1 -Mode ci
```

**Output:**
```
frontend/src/api/generated/hydrostatics.types.ts
frontend/src/api/generated/sizing.types.ts
frontend/src/api/generated/identity.types.ts
frontend/src/api/generated/index.ts
```

---

## 🔄 Workflow

### When Backend DTOs Change

```bash
# 1. Update C# DTO
# File: backend/Shared/DTOs/VesselDto.cs
public record VesselDto
{
    [Convertible("Length")]
    public decimal NewPropertyM { get; init; }  // ← NEW
}

# 2. Start service
cd backend/DataService
dotnet run

# 3. Export swagger
pwsh scripts/export-swagger.ps1

# 4. Generate types
pwsh scripts/generate-types.ps1 -Mode ci

# 5. Frontend now knows about new property!
import { VesselDto } from '@/api/generated';
// TypeScript autocomplete shows: newPropertyM

# 6. Commit all changes
git add backend/Shared/DTOs/
git add backend/*/swagger.json
git add frontend/src/api/generated/
git commit -m "feat: add new property to vessel DTO"
```

---

## ✅ CI/CD Validation

### Workflow: `.github/workflows/validate-types.yml`

**Triggers:**
- Pull requests
- DTO file changes
- swagger.json changes

**Validates:**
1. Regenerates types from swagger.json
2. Compares with committed types
3. Fails if mismatch
4. Runs TypeScript type-check

**Result:**
- Frontend always matches backend
- No runtime type errors
- Safe refactoring

---

## 💡 NPM Scripts

```bash
# Generate types (local dev)
npm run generate:types

# Generate types (CI mode)
npm run generate:types:ci

# Export swagger specs
npm run export:swagger
```

---

## 🐛 Troubleshooting

### NSwag Not Found

```bash
dotnet tool install -g NSwag.ConsoleCore --no-cache
```

### Service Not Running

**Option 1:** Start service
```bash
cd backend/DataService
dotnet run
```

**Option 2:** Use CI mode
```bash
pwsh scripts/generate-types.ps1 -Mode ci
```

### Types Out of Sync (PR Failed)

```bash
# Regenerate
pwsh scripts/export-swagger.ps1
pwsh scripts/generate-types.ps1 -Mode ci

# Commit
git add backend/*/swagger.json
git add frontend/src/api/generated/
git commit --amend --no-edit
git push --force-with-lease
```

---

## 📊 Type Mapping

| C# Type | TypeScript Type |
|---------|-----------------|
| `string` | `string` |
| `int`, `decimal` | `number` |
| `bool` | `boolean` |
| `DateTime` | `string` (ISO 8601) |
| `Guid` | `string` (UUID) |
| `decimal?` | `number \| undefined` |
| `List<T>` | `T[]` |
| `enum` | `enum` |

---

## 🎓 Best Practices

1. **Always commit generated files**
2. **Regenerate after every DTO change**
3. **Use re-exports for convenience**
4. **Keep custom types separate**
5. **Review generated types in PRs**

---

## 📞 Support

Questions? Check:
1. This documentation
2. `frontend/src/api/generated/README.md`
3. Error messages
4. #engineering channel

---

**Last Updated:** 2025-01-04
