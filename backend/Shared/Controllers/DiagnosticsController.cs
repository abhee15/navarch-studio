using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Shared.Controllers;

/// <summary>
/// Diagnostics controller for version and health information
/// Can be inherited by services to provide standard diagnostic endpoints
/// </summary>
[ApiController]
[Route("[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly ILogger<DiagnosticsController> _logger;
    private readonly IConfiguration _configuration;

    public DiagnosticsController(
        ILogger<DiagnosticsController> logger,
        IConfiguration configuration)
    {
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
            serviceName = assembly.GetName().Name,
            version = assembly.GetName().Version?.ToString() ?? "1.0.0",
            commitSha = commitSha,
            buildDate = buildDate.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            environment = environment,
            framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            os = System.Runtime.InteropServices.RuntimeInformation.OSDescription
        };

        _logger.LogInformation("Version endpoint called: {ServiceName} v{Version} ({Environment})",
            version.serviceName, version.version, version.environment);

        return Ok(version);
    }
}

