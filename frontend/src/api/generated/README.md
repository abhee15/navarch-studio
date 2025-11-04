# Generated TypeScript Types

⚠️ **DO NOT EDIT FILES IN THIS DIRECTORY MANUALLY**

These TypeScript interfaces are automatically generated from backend C# DTOs using NSwag.

## Regenerating Types

### Local Development
```bash
# From project root
pwsh scripts/generate-types.ps1

# Or from frontend directory
npm run generate:types
```

### CI/CD
```bash
# Uses committed swagger.json files
pwsh scripts/generate-types.ps1 -Mode ci

# Or
npm run generate:types:ci
```

## How It Works

1. **Export**: Backend services export Swagger/OpenAPI spec to `swagger.json`
2. **Generate**: NSwag reads spec and generates TypeScript interfaces
3. **Commit**: Generated types are committed to git
4. **Use**: Frontend imports and uses the types

## Benefits

- ✅ **No manual sync** - Types auto-update from backend
- ✅ **Type safety** - Compiler catches API contract changes
- ✅ **Autocomplete** - Better IDE support
- ✅ **CI/CD ready** - Works in GitHub Actions without running services

## File Organization

```
frontend/src/api/generated/
├── hydrostatics.types.ts  # From DataService
├── sizing.types.ts        # From HullSizingService  
├── identity.types.ts      # From IdentityService
├── index.ts               # Re-exports all types
└── README.md              # This file
```

## Troubleshooting

**Service not running (Local mode)**
```bash
cd backend/DataService
dotnet run
```

**Swagger file not found (CI mode)**
```bash
pwsh scripts/export-swagger.ps1
```

## Full Documentation

See [docs/DTO-AUTOMATION.md](../../../docs/DTO-AUTOMATION.md) for complete guide.

