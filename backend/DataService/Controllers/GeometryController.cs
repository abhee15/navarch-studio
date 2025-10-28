using Asp.Versioning;
using DataService.Services.Hydrostatics;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace DataService.Controllers;

/// <summary>
/// Controller for hull geometry management (stations, waterlines, offsets)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hydrostatics/vessels/{vesselId}")]
public class GeometryController : ControllerBase
{
    private readonly IGeometryService _geometryService;
    private readonly ICsvParserService _csvParserService;
    private readonly ILogger<GeometryController> _logger;

    public GeometryController(
        IGeometryService geometryService,
        ICsvParserService csvParserService,
        ILogger<GeometryController> logger)
    {
        _geometryService = geometryService;
        _csvParserService = csvParserService;
        _logger = logger;
    }

    /// <summary>
    /// Imports stations for a vessel
    /// </summary>
    [HttpPost("stations")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportStations(
        Guid vesselId,
        [FromBody] ImportStationsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var stations = await _geometryService.ImportStationsAsync(
                vesselId,
                request.Stations,
                cancellationToken);

            return CreatedAtAction(nameof(GetOffsetsGrid), new { vesselId }, new { imported = stations.Count });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error importing stations for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Imports waterlines for a vessel
    /// </summary>
    [HttpPost("waterlines")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportWaterlines(
        Guid vesselId,
        [FromBody] ImportWaterlinesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var waterlines = await _geometryService.ImportWaterlinesAsync(
                vesselId,
                request.Waterlines,
                cancellationToken);

            return CreatedAtAction(nameof(GetOffsetsGrid), new { vesselId }, new { imported = waterlines.Count });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error importing waterlines for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Bulk imports offsets for a vessel
    /// </summary>
    [HttpPost("offsets:bulk")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkImportOffsets(
        Guid vesselId,
        [FromBody] ImportOffsetsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var offsets = await _geometryService.ImportOffsetsAsync(
                vesselId,
                request.Offsets,
                cancellationToken);

            return CreatedAtAction(nameof(GetOffsetsGrid), new { vesselId }, new { imported = offsets.Count });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error importing offsets for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Imports all geometry (stations, waterlines, offsets) in one transaction
    /// </summary>
    [HttpPost("geometry:import")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportCombinedGeometry(
        Guid vesselId,
        [FromBody] ImportCombinedGeometryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var (stationsCount, waterlinesCount, offsetsCount) = await _geometryService.ImportCombinedGeometryAsync(
                vesselId,
                request.Stations,
                request.Waterlines,
                request.Offsets,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetOffsetsGrid),
                new { vesselId },
                new
                {
                    stations_imported = stationsCount,
                    waterlines_imported = waterlinesCount,
                    offsets_imported = offsetsCount,
                    validation_errors = new List<string>()
                });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error importing combined geometry for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the complete offsets grid for a vessel
    /// </summary>
    [HttpGet("offsets")]
    [ProducesResponseType(typeof(OffsetsGridDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOffsetsGrid(
        Guid vesselId,
        CancellationToken cancellationToken)
    {
        var grid = await _geometryService.GetOffsetsGridAsync(vesselId, cancellationToken);

        if (grid == null)
        {
            return NotFound(new { error = $"No geometry data found for vessel {vesselId}" });
        }

        return Ok(grid);
    }

    /// <summary>
    /// Uploads and imports geometry from a CSV file
    /// </summary>
    [HttpPost("offsets:upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadCsv(
        Guid vesselId,
        IFormFile file,
        [FromForm] string format, // "combined" or "offsets_only"
        CancellationToken cancellationToken)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "File must be a CSV" });
            }

            using var stream = file.OpenReadStream();

            if (format == "combined")
            {
                // Parse combined format (station_index, station_x, waterline_index, waterline_z, half_breadth_y)
                var geometry = await _csvParserService.ParseCombinedOffsetsAsync(stream, cancellationToken);

                var (stationsCount, waterlinesCount, offsetsCount) = await _geometryService.ImportCombinedGeometryAsync(
                    vesselId,
                    geometry.Stations,
                    geometry.Waterlines,
                    geometry.Offsets,
                    cancellationToken);

                return CreatedAtAction(
                    nameof(GetOffsetsGrid),
                    new { vesselId },
                    new
                    {
                        stations_imported = stationsCount,
                        waterlines_imported = waterlinesCount,
                        offsets_imported = offsetsCount,
                        validation_errors = new List<string>()
                    });
            }
            else if (format == "offsets_only")
            {
                // Parse offsets only (station_index, waterline_index, half_breadth_y)
                var offsets = await _csvParserService.ParseOffsetsAsync(stream, cancellationToken);

                var importedOffsets = await _geometryService.ImportOffsetsAsync(
                    vesselId,
                    offsets,
                    cancellationToken);

                return CreatedAtAction(
                    nameof(GetOffsetsGrid),
                    new { vesselId },
                    new
                    {
                        stations_imported = 0,
                        waterlines_imported = 0,
                        offsets_imported = importedOffsets.Count,
                        validation_errors = new List<string>()
                    });
            }
            else
            {
                return BadRequest(new { error = $"Unknown format: {format}. Use 'combined' or 'offsets_only'" });
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error uploading CSV for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Downloads a CSV template file (independent of vessel - does not use vesselId)
    /// </summary>
    /// <param name="templateName">Template name: stations, waterlines, offsets, offsets_only, or offsets_template</param>
    /// <param name="vesselId">Not used - templates are global</param>
    [HttpGet("~/api/v{version:apiVersion}/hydrostatics/templates/{templateName}")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DownloadTemplate(string templateName, Guid vesselId = default)
    {
        var templateFiles = new Dictionary<string, string>
        {
            ["stations"] = "stations_template.csv",
            ["waterlines"] = "waterlines_template.csv",
            ["offsets"] = "offsets_template.csv",
            ["offsets_only"] = "offsets_only_template.csv",
            ["offsets_template"] = "offsets_template.csv"
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

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        return File(fileBytes, "text/csv", fileName);
    }
}

/// <summary>
/// Request model for importing stations
/// </summary>
public record ImportStationsRequest
{
    public List<StationDto> Stations { get; init; } = new();
}

/// <summary>
/// Request model for importing waterlines
/// </summary>
public record ImportWaterlinesRequest
{
    public List<WaterlineDto> Waterlines { get; init; } = new();
}

/// <summary>
/// Request model for importing offsets
/// </summary>
public record ImportOffsetsRequest
{
    public List<OffsetDto> Offsets { get; init; } = new();
}

/// <summary>
/// Request model for importing all geometry at once
/// </summary>
public record ImportCombinedGeometryRequest
{
    public List<StationDto> Stations { get; init; } = new();
    public List<WaterlineDto> Waterlines { get; init; } = new();
    public List<OffsetDto> Offsets { get; init; } = new();
}

