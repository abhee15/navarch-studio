# Database Conventions

## Naming Convention Standard

**CRITICAL**: All database schemas, tables, and columns MUST use `snake_case` naming convention.

### Why Snake_Case?

- **PostgreSQL Best Practice**: snake_case is the standard convention in PostgreSQL
- **Case Sensitivity**: PostgreSQL treats unquoted identifiers as lowercase, making snake_case natural
- **Consistency**: All existing data tables use snake_case (see `data` schema)
- **EF Core Integration**: `.UseSnakeCaseNamingConvention()` automatically handles C# ↔ PostgreSQL mapping

### Enforcement

All DbContext configurations MUST include:

```csharp
builder.Services.AddDbContext<YourDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // ... other configuration ...
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "your_schema");
    })
    .UseSnakeCaseNamingConvention()  // ✅ REQUIRED
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment());
});
```

### Examples

#### ✅ CORRECT (snake_case)

**Tables:**
- `users`
- `vessel_metadata`
- `loading_conditions`
- `hydro_results`

**Columns:**
- `id`
- `created_at`
- `updated_at`
- `source_catalog_hull_id`
- `preferred_units`

**Schemas:**
- `identity`
- `data`
- `analytics`

#### ❌ INCORRECT (PascalCase/camelCase)

**Tables:**
- `Users` ❌
- `VesselMetadata` ❌
- `loadingConditions` ❌

**Columns:**
- `Id` ❌
- `CreatedAt` ❌
- `UpdatedAt` ❌
- `SourceCatalogHullId` ❌
- `preferredUnits` ❌

### C# Model Mapping

In C# models, continue using PascalCase - EF Core with snake_case convention handles the mapping:

```csharp
// C# Model (PascalCase)
public class User
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string PreferredUnits { get; set; }  // Maps to preferred_units
    public DateTime CreatedAt { get; set; }      // Maps to created_at
}

// PostgreSQL Table (snake_case)
// identity.users
// - id
// - email
// - preferred_units
// - created_at
```

### Migration Checklist

When creating new migrations:

1. ✅ Ensure `UseSnakeCaseNamingConvention()` is in DbContext configuration
2. ✅ Generate migration: `dotnet ef migrations add MigrationName`
3. ✅ Review migration file - verify snake_case column names
4. ✅ Test locally before deploying
5. ✅ Apply to local: `dotnet ef database update`
6. ✅ Verify schema matches code expectations

### Schema-Specific Rules

**Identity Schema** (`identity`)
- Purpose: User authentication, authorization
- Tables: `users`, `roles`, `permissions` (if added)
- Owner: IdentityService
- Migration History: `identity.__EFMigrationsHistory`

**Data Schema** (`data`)
- Purpose: Application domain data (vessels, hydrostatics, etc.)
- Tables: `vessels`, `loadcases`, `hydro_results`, etc.
- Owner: DataService
- Migration History: `data.__EFMigrationsHistory`

### Troubleshooting

**Error: `relation "schema.TableName" does not exist`**
- Cause: Code expects PascalCase but database has snake_case (or vice versa)
- Fix: Ensure `.UseSnakeCaseNamingConvention()` is present in DbContext config
- Verify: Check actual table names with `\dt schema.*` in psql

**Error: `column table.ColumnName does not exist`**
- Cause: Column naming mismatch
- Fix: Regenerate migrations with correct naming convention
- Verify: Check columns with `\d schema.table_name` in psql

### Fresh Start (Local Development)

If you need to reset local database to snake_case:

```powershell
# 1. Stop services
docker compose down

# 2. Remove database volume
docker volume rm navarch-studio_postgres_data

# 3. Start fresh
docker compose up -d postgres

# 4. Apply migrations (will create snake_case tables)
./scripts/setup-local-db.ps1
```

### Cloud Deployments

- ✅ **Production databases** already use snake_case (created by migrations)
- ✅ **Auto-migration enabled** in Staging/Production environments
- ✅ New migrations will maintain snake_case consistency
- ⚠️ **Breaking Change**: If switching existing PascalCase to snake_case, requires:
  - Data migration
  - Downtime window
  - Rollback plan

### When Adding New Services

Any new microservice that uses PostgreSQL MUST:

1. Add `UseSnakeCaseNamingConvention()` in DbContext setup
2. Use dedicated schema (not `public`)
3. Configure separate migrations history table
4. Follow snake_case in all migrations

### References

- [EF Core Naming Conventions](https://github.com/efcore/EFCore.NamingConventions)
- [PostgreSQL Naming Conventions](https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-SYNTAX-IDENTIFIERS)
- Current implementations:
  - `backend/IdentityService/Program.cs` (lines 71-95)
  - `backend/DataService/Program.cs` (lines 107-133)

