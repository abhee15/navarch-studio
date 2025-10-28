using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace DataService.Controllers;

/// <summary>
/// Controller for serving CSV template files
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hydrostatics/templates")]
public class TemplatesController : ControllerBase
{
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(ILogger<TemplatesController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Downloads a CSV template file
    /// </summary>
    /// <param name="templateName">Template name: stations, waterlines, offsets, offsets_only, or combined</param>
    [HttpGet("{templateName}")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DownloadTemplate(string templateName)
    {
        var templateFiles = new Dictionary<string, string>
        {
            ["stations"] = "stations_template.csv",
            ["waterlines"] = "waterlines_template.csv",
            ["offsets"] = "offsets_template.csv",
            ["offsets_only"] = "offsets_only_template.csv",
            ["combined"] = "offsets_template.csv" // Combined format uses full offsets template
        };

        if (!templateFiles.TryGetValue(templateName.ToLower(), out var fileName))
        {
            return NotFound(new { error = $"Template '{templateName}' not found. Available templates: {string.Join(", ", templateFiles.Keys)}" });
        }

        var filePath = Path.Combine(AppContext.BaseDirectory, "templates", fileName);

        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogError("Template file not found at path: {FilePath}", filePath);
            return NotFound(new { error = $"Template file '{fileName}' not found on server" });
        }

        _logger.LogInformation("Serving template file: {FileName}", fileName);
        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        return File(fileBytes, "text/csv", fileName);
    }

    /// <summary>
    /// Lists all available templates
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ListTemplates()
    {
        var templates = new[]
        {
            new
            {
                name = "stations",
                fileName = "stations_template.csv",
                description = "Template for importing station positions (X coordinates along hull length)",
                endpoint = "/api/v1/hydrostatics/templates/stations"
            },
            new
            {
                name = "waterlines",
                fileName = "waterlines_template.csv",
                description = "Template for importing waterline positions (Z coordinates - vertical heights)",
                endpoint = "/api/v1/hydrostatics/templates/waterlines"
            },
            new
            {
                name = "offsets",
                fileName = "offsets_template.csv",
                description = "Template for importing offsets with stations and waterlines (combined format)",
                endpoint = "/api/v1/hydrostatics/templates/offsets"
            },
            new
            {
                name = "offsets_only",
                fileName = "offsets_only_template.csv",
                description = "Template for importing only offset values (requires existing stations and waterlines)",
                endpoint = "/api/v1/hydrostatics/templates/offsets_only"
            },
            new
            {
                name = "combined",
                fileName = "offsets_template.csv",
                description = "Combined format with all geometry data (stations, waterlines, and offsets)",
                endpoint = "/api/v1/hydrostatics/templates/combined"
            }
        };

        return Ok(new { templates });
    }
}

