# Generated TypeScript Types

⚠️ **DO NOT EDIT FILES IN THIS DIRECTORY MANUALLY**

These TypeScript interfaces are automatically generated from backend C# DTOs using NSwag.

## Getting Started

To generate types for the first time:

```bash
# 1. Start backend services
cd backend/DataService
dotnet run

# 2. Export swagger.json files
pwsh scripts/export-swagger.ps1

# 3. Generate TypeScript types
pwsh scripts/generate-types.ps1 -Mode ci
```

## Regenerating Types

### Local Development
```bash
npm run generate:types
```

### CI/CD
```bash
npm run generate:types:ci
```

## How It Works

1. Backend services export Swagger/OpenAPI spec
2. NSwag reads spec and generates TypeScript interfaces
3. Generated types are committed to git
4. CI validates types stay in sync

## Documentation

See [docs/DTO-AUTOMATION.md](../../../../docs/DTO-AUTOMATION.md) for complete guide.
