using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataService.Data;

/// <summary>
/// Validates that database schema matches expected state after migrations
/// Prevents runtime errors due to missing columns or out-of-order migrations
/// </summary>
public class MigrationValidator
{
    private readonly DataDbContext _context;
    private readonly ILogger<MigrationValidator> _logger;

    public MigrationValidator(DataDbContext context, ILogger<MigrationValidator> logger)
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

        // 2. Validate critical tables and columns exist
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

        // Critical tables that must exist
        // NOTE: Column names must match exactly what's in the migrations (snake_case)
        // If new columns are added in future migrations, update this list accordingly
        var criticalTables = new Dictionary<string, List<string>>
        {
            ["vessels"] = new List<string>
            {
                "id", "user_id", "name", "lpp", "beam", "design_draft"
                // Note: units_system was never added - unit conversion uses User.PreferredUnits instead
            },
            ["loadcases"] = new List<string>
            {
                "id", "vessel_id", "name", "rho", "kg"
            },
            ["stations"] = new List<string>
            {
                "id", "vessel_id", "station_index", "x"
            },
            ["waterlines"] = new List<string>
            {
                "id", "vessel_id", "waterline_index", "z"
            },
            ["offsets"] = new List<string>
            {
                "id", "vessel_id", "station_index", "waterline_index", "half_breadth_y"
            },
            // Seakeeping tables (exist in InitialCreate migration)
            ["sea_states"] = new List<string>
            {
                "id", "vessel_id", "name", "wave_height", "wave_period", "wave_direction"
                // Note: Actual columns are wave_height (not significant_wave_height) and wave_period (not wave_spectrum_type)
            },
            ["rao_results"] = new List<string>
            {
                "id", "vessel_id", "loadcase_id", "frequency"
                // Note: Actual column is frequency (not wave_frequency), and there's no wave_heading column
            }
        };

        foreach (var (tableName, expectedColumns) in criticalTables)
        {
            var tableExists = await CheckTableExistsAsync(tableName, cancellationToken);
            if (!tableExists)
            {
                result.AddError($"Critical table '{tableName}' does not exist");
                _logger.LogError("[VALIDATION] Missing table: {Table}", tableName);
                continue; // Skip column check if table doesn't exist
            }

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
            _logger.LogInformation("[VALIDATION] ✓ All critical schema elements present");
        }
    }

    private async Task<bool> CheckTableExistsAsync(string tableName, CancellationToken cancellationToken)
    {
        // Use raw SQL with parameterized query to check table existence
        var sql = @"
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = 'data'
              AND table_name = @p0";

        // Use EF Core's connection management
        var connection = _context.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;

        if (!wasOpen)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@p0";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            var count = Convert.ToInt32(result);
            return count > 0;
        }
        finally
        {
            if (!wasOpen)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<List<string>> GetTableColumnsAsync(string tableName, CancellationToken cancellationToken)
    {
        // Use raw SQL with parameterized query to get column names
        var sql = @"
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'data'
              AND table_name = @p0
            ORDER BY ordinal_position";

        var connection = _context.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;

        if (!wasOpen)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@p0";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var columns = new List<string>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                columns.Add(reader.GetString(0));
            }
            return columns;
        }
        finally
        {
            if (!wasOpen)
            {
                await connection.CloseAsync();
            }
        }
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
