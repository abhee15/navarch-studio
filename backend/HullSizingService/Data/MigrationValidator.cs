using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HullSizingService.Data;

/// <summary>
/// Validates that database schema matches expected state after migrations
/// Prevents runtime errors due to missing columns or out-of-order migrations
/// </summary>
public class MigrationValidator
{
    private readonly SizingDbContext _context;
    private readonly ILogger<MigrationValidator> _logger;

    public MigrationValidator(SizingDbContext context, ILogger<MigrationValidator> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Validates all migrations are applied in order and schema matches expectations
    /// </summary>
    public async Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        // 1. Check for out-of-order migrations
        await CheckMigrationOrderAsync(result, cancellationToken);

        // 2. Validate critical columns exist
        await ValidateCriticalColumnsAsync(result, cancellationToken);

        // 3. Validate constraints exist
        await ValidateConstraintsAsync(result, cancellationToken);

        return result;
    }

    private async Task CheckMigrationOrderAsync(ValidationResult result, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[VALIDATION] Checking migration order...");

        var appliedMigrations = (await _context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
        var allMigrations = _context.Database.GetMigrations().ToList();

        // Check if all code migrations are in database
        var missingMigrations = allMigrations.Except(appliedMigrations).ToList();
        if (missingMigrations.Any())
        {
            result.AddError($"Missing migrations: {string.Join(", ", missingMigrations)}");
            _logger.LogError("[VALIDATION] Missing migrations detected: {Migrations}", string.Join(", ", missingMigrations));
        }

        // Check for out-of-order application
        var sortedCodeMigrations = allMigrations.OrderBy(m => m).ToList();
        var sortedAppliedMigrations = appliedMigrations.OrderBy(m => m).ToList();

        for (int i = 0; i < sortedAppliedMigrations.Count && i < sortedCodeMigrations.Count; i++)
        {
            if (sortedAppliedMigrations[i] != sortedCodeMigrations[i])
            {
                result.AddWarning($"Migration order mismatch at position {i}: Expected '{sortedCodeMigrations[i]}', Found '{sortedAppliedMigrations[i]}'");
                _logger.LogWarning("[VALIDATION] Migration order mismatch detected");
            }
        }

        if (result.Errors.Count == 0)
        {
            _logger.LogInformation("[VALIDATION] ✓ Migration order is correct");
        }
    }

    private async Task ValidateCriticalColumnsAsync(ValidationResult result, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[VALIDATION] Checking critical columns...");

        var criticalColumns = new Dictionary<string, List<string>>
        {
            ["sizing_runs"] = new List<string>
            {
                "id", "mission_case_id", "mode", "status", "created_at",
                "diagnostics_json", // Added in 20251108000000_AddDiagnosticsToSizingRuns
                "length", "breadth", "draft", "displacement",
                "block_coefficient", "prismatic_coefficient", "waterplane_coefficient",
                "bm", "gm", "brief_name"
            },
            ["mission_cases"] = new List<string>
            {
                "id", "user_id", "name", "vessel_type", "speed", "range", "cargo_weight"
            }
        };

        foreach (var (tableName, expectedColumns) in criticalColumns)
        {
            var actualColumns = await GetTableColumnsAsync(tableName, cancellationToken);

            foreach (var expectedColumn in expectedColumns)
            {
                if (!actualColumns.Contains(expectedColumn))
                {
                    result.AddError($"Table '{tableName}' is missing critical column '{expectedColumn}'");
                    _logger.LogError("[VALIDATION] Missing column: {Table}.{Column}", tableName, expectedColumn);
                }
            }
        }

        if (result.Errors.Count == 0)
        {
            _logger.LogInformation("[VALIDATION] ✓ All critical columns present");
        }
    }

    private async Task ValidateConstraintsAsync(ValidationResult result, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[VALIDATION] Checking constraints...");

        // Check for critical constraints
        var hasUniqueConstraint = await CheckConstraintExistsAsync(
            "sizing_runs",
            "ak_sizing_runs_brief_name",
            cancellationToken);

        if (!hasUniqueConstraint)
        {
            result.AddWarning("UNIQUE constraint 'ak_sizing_runs_brief_name' is missing (may cause duplicate brief names)");
            _logger.LogWarning("[VALIDATION] Missing UNIQUE constraint on sizing_runs.brief_name");
        }

        // Check for check constraints
        var hasGmConstraint = await CheckConstraintExistsAsync(
            "sizing_runs",
            "ck_sizing_runs_gm_non_negative",
            cancellationToken);

        if (!hasGmConstraint)
        {
            result.AddWarning("CHECK constraint 'ck_sizing_runs_gm_non_negative' is missing");
            _logger.LogWarning("[VALIDATION] Missing CHECK constraint for GM");
        }

        if (result.Warnings.Count == 0)
        {
            _logger.LogInformation("[VALIDATION] ✓ All critical constraints present");
        }
    }

    private async Task<List<string>> GetTableColumnsAsync(string tableName, CancellationToken cancellationToken)
    {
        var sql = @"
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'sizing'
              AND table_name = {0}
            ORDER BY ordinal_position";

        var columns = await _context.Database
            .SqlQueryRaw<string>(sql, tableName)
            .ToListAsync(cancellationToken);

        return columns;
    }

    private async Task<bool> CheckConstraintExistsAsync(string tableName, string constraintName, CancellationToken cancellationToken)
    {
        var sql = @"
            SELECT COUNT(*)
            FROM information_schema.table_constraints
            WHERE table_schema = 'sizing'
              AND table_name = {0}
              AND constraint_name = {1}";

        var count = await _context.Database
            .SqlQueryRaw<int>(sql, tableName, constraintName)
            .FirstOrDefaultAsync(cancellationToken);

        return count > 0;
    }
}

public class ValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public bool IsValid => Errors.Count == 0;
    public bool HasWarnings => Warnings.Count > 0;

    public void AddError(string error) => Errors.Add(error);
    public void AddWarning(string warning) => Warnings.Add(warning);

    public void LogResults(ILogger logger)
    {
        if (IsValid && !HasWarnings)
        {
            logger.LogInformation("✅ [VALIDATION] All checks passed");
            return;
        }

        if (Errors.Count > 0)
        {
            logger.LogError("❌ [VALIDATION] {Count} error(s) found:", Errors.Count);
            foreach (var error in Errors)
            {
                logger.LogError("   - {Error}", error);
            }
        }

        if (Warnings.Count > 0)
        {
            logger.LogWarning("⚠️  [VALIDATION] {Count} warning(s) found:", Warnings.Count);
            foreach (var warning in Warnings)
            {
                logger.LogWarning("   - {Warning}", warning);
            }
        }
    }
}
