using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataService.Data;
using System.Reflection;

namespace DataService.Controllers;

/// <summary>
/// Diagnostics endpoint for Data Service
/// Provides version, database connectivity, and migration status
/// </summary>
[ApiController]
[Route("[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly DataDbContext _dbContext;
    private readonly ILogger<DiagnosticsController> _logger;
    private readonly IConfiguration _configuration;

    public DiagnosticsController(
        DataDbContext dbContext,
        ILogger<DiagnosticsController> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Returns version information for the service
    /// </summary>
    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        var buildDate = System.IO.File.GetLastWriteTime(assembly!.Location);
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown";

        // Try to get commit SHA from environment variable (set during build)
        var commitSha = _configuration["GIT_COMMIT_SHA"] ?? "unknown";

        var version = new
        {
            serviceName = "DataService",
            version = assembly.GetName().Version?.ToString() ?? "1.0.0",
            commitSha = commitSha,
            buildDate = buildDate.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            environment = environment,
            framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            os = System.Runtime.InteropServices.RuntimeInformation.OSDescription
        };

        _logger.LogInformation("[DIAGNOSTICS] Version endpoint called: {ServiceName} v{Version} ({Environment})",
            version.serviceName, version.version, version.environment);

        return Ok(version);
    }

    /// <summary>
    /// Returns comprehensive diagnostics including database status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[DIAGNOSTICS] Status endpoint called");

        var database = await GetDatabaseStatus(cancellationToken);
        var migrations = await GetMigrationStatus(cancellationToken);
        var tables = await GetTableStatus(cancellationToken);

        var diagnostics = new
        {
            service = "DataService",
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown",
            database = database,
            migrations = migrations,
            tables = tables
        };

        _logger.LogInformation("[DIAGNOSTICS] Status check complete");

        return Ok(diagnostics);
    }

    private async Task<object> GetDatabaseStatus(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            var connectionString = _dbContext.Database.GetConnectionString();

            // Redact password from connection string for logging
            var sanitizedConnectionString = connectionString?
                .Replace(_configuration["DatabasePassword"] ?? "", "***")
                .Replace("Password=", "Password=***;");

            return new
            {
                connected = canConnect,
                connectionString = sanitizedConnectionString,
                provider = _dbContext.Database.ProviderName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DIAGNOSTICS] Database connection check failed");
            return new
            {
                connected = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            };
        }
    }

    private async Task<object> GetMigrationStatus(CancellationToken cancellationToken)
    {
        try
        {
            var appliedMigrations = await _dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);

            return new
            {
                appliedCount = appliedMigrations.Count(),
                pendingCount = pendingMigrations.Count(),
                appliedMigrations = appliedMigrations.ToList(),
                pendingMigrations = pendingMigrations.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DIAGNOSTICS] Migration status check failed");
            return new
            {
                error = ex.Message,
                stackTrace = ex.StackTrace
            };
        }
    }

    private async Task<object> GetTableStatus(CancellationToken cancellationToken)
    {
        try
        {
            // Check if vessels table exists by trying to query it
            var vesselsTableExists = false;
            try
            {
                await _dbContext.Vessels.AnyAsync(cancellationToken);
                vesselsTableExists = true;
            }
            catch
            {
                // Table doesn't exist
            }

            return new
            {
                vesselTableExists = vesselsTableExists,
                note = "Table count check removed due to SqlQueryRaw compatibility issues"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DIAGNOSTICS] Table status check failed");
            return new
            {
                error = ex.Message,
                stackTrace = ex.StackTrace
            };
        }
    }
}
