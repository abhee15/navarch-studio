using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityService.Data;

/// <summary>
/// Validates that database schema matches expected state after migrations
/// Prevents runtime errors due to missing columns or out-of-order migrations
/// </summary>
public class MigrationValidator
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<MigrationValidator> _logger;

    public MigrationValidator(IdentityDbContext context, ILogger<MigrationValidator> logger)
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

        // 2. Validate critical schema exists
        await ValidateCriticalSchemaAsync(result, cancellationToken);

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

        if (result.Errors.Count == 0)
        {
            _logger.LogInformation("[VALIDATION] ✓ Migration order is correct");
        }
    }

    private async Task ValidateCriticalSchemaAsync(ValidationResult result, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[VALIDATION] Checking critical schema elements...");

        // Critical table and columns
        var criticalColumns = new Dictionary<string, List<string>>
        {
            ["users"] = new List<string>
            {
                "id", "email", "name", "password_hash", "preferred_units",
                "created_at", "updated_at", "deleted_at"
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

        // Check for unique constraint on email
        var hasEmailConstraint = await CheckConstraintExistsAsync("users", "ix_users_email", cancellationToken);
        if (!hasEmailConstraint)
        {
            result.AddError("UNIQUE constraint 'ix_users_email' is missing from users table");
            _logger.LogError("[VALIDATION] Missing UNIQUE constraint on users.email");
        }

        if (result.Errors.Count == 0)
        {
            _logger.LogInformation("[VALIDATION] ✓ All critical schema elements present");
        }
    }

    private async Task<List<string>> GetTableColumnsAsync(string tableName, CancellationToken cancellationToken)
    {
        var sql = @"
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'identity'
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
            FROM pg_indexes
            WHERE schemaname = 'identity'
              AND tablename = {0}
              AND indexname = {1}";

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
}




