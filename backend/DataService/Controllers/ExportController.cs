using System.Diagnostics;
using Asp.Versioning;
using DataService.Services.Hydrostatics;
using Microsoft.AspNetCore.Mvc;

namespace DataService.Controllers;

/// <summary>
/// Controller for exporting hydrostatic data
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hydrostatics/vessels/{vesselId}/export")]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly IHydroCalculator _hydroCalculator;
    private readonly ICurvesGenerator _curvesGenerator;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        IExportService exportService,
        IHydroCalculator hydroCalculator,
        ICurvesGenerator curvesGenerator,
        ILogger<ExportController> logger)
    {
        _exportService = exportService;
        _hydroCalculator = hydroCalculator;
        _curvesGenerator = curvesGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Exports hydrostatic table to CSV
    /// </summary>
    [HttpPost("csv/table")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportTableToCsv(
        Guid vesselId,
        [FromBody] ExportTableRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Compute hydrostatic table
            var results = await _hydroCalculator.ComputeTableAsync(
                vesselId,
                request.LoadcaseId,
                request.Drafts,
                cancellationToken);

            // Export to CSV
            var csvData = await _exportService.ExportToCsvAsync(results, cancellationToken);

            _logger.LogInformation(
                "Exported hydrostatic table for vessel {VesselId} to CSV ({Size} bytes)",
                vesselId, csvData.Length);

            return File(csvData, "text/csv", $"hydrostatics_{vesselId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting hydrostatic table to CSV for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Exports hydrostatic table to JSON
    /// </summary>
    [HttpPost("json/table")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportTableToJson(
        Guid vesselId,
        [FromBody] ExportTableRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Compute hydrostatic table
            var results = await _hydroCalculator.ComputeTableAsync(
                vesselId,
                request.LoadcaseId,
                request.Drafts,
                cancellationToken);

            // Export to JSON
            var jsonData = await _exportService.ExportToJsonAsync(results, cancellationToken);

            _logger.LogInformation(
                "Exported hydrostatic table for vessel {VesselId} to JSON ({Size} bytes)",
                vesselId, jsonData.Length);

            return File(jsonData, "application/json", $"hydrostatics_{vesselId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting hydrostatic table to JSON for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Exports curves to CSV
    /// </summary>
    [HttpPost("csv/curves")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportCurvesToCsv(
        Guid vesselId,
        [FromBody] ExportCurvesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Generate curves
            var curves = await GenerateCurvesAsync(
                vesselId,
                request.LoadcaseId,
                request.CurveTypes,
                request.MinDraft,
                request.MaxDraft,
                request.Points,
                cancellationToken);

            // Export to CSV
            var csvData = await _exportService.ExportCurvesToCsvAsync(curves, cancellationToken);

            _logger.LogInformation(
                "Exported {Count} curves for vessel {VesselId} to CSV ({Size} bytes)",
                curves.Count, vesselId, csvData.Length);

            return File(csvData, "text/csv", $"curves_{vesselId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting curves to CSV for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Exports curves to JSON
    /// </summary>
    [HttpPost("json/curves")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportCurvesToJson(
        Guid vesselId,
        [FromBody] ExportCurvesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Generate curves
            var curves = await GenerateCurvesAsync(
                vesselId,
                request.LoadcaseId,
                request.CurveTypes,
                request.MinDraft,
                request.MaxDraft,
                request.Points,
                cancellationToken);

            // Export to JSON
            var jsonData = await _exportService.ExportCurvesToJsonAsync(curves, cancellationToken);

            _logger.LogInformation(
                "Exported {Count} curves for vessel {VesselId} to JSON ({Size} bytes)",
                curves.Count, vesselId, jsonData.Length);

            return File(jsonData, "application/json", $"curves_{vesselId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting curves to JSON for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Exports comprehensive report to PDF
    /// </summary>
    [HttpPost("pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportToPdf(
        Guid vesselId,
        [FromBody] ExportReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            var pdfData = await _exportService.ExportToPdfAsync(
                vesselId,
                request.LoadcaseId,
                request.IncludeCurves,
                cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Exported PDF report for vessel {VesselId} ({Size} bytes, {ElapsedMs}ms)",
                vesselId, pdfData.Length, stopwatch.ElapsedMilliseconds);

            return File(pdfData, "application/pdf", $"hydrostatics_{vesselId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting PDF report for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Exports comprehensive report to Excel
    /// </summary>
    [HttpPost("excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportToExcel(
        Guid vesselId,
        [FromBody] ExportReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            var excelData = await _exportService.ExportToExcelAsync(
                vesselId,
                request.LoadcaseId,
                request.IncludeCurves,
                cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Exported Excel report for vessel {VesselId} ({Size} bytes, {ElapsedMs}ms)",
                vesselId, excelData.Length, stopwatch.ElapsedMilliseconds);

            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"hydrostatics_{vesselId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting Excel report for vessel {VesselId}", vesselId);
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<List<Shared.DTOs.CurveDto>> GenerateCurvesAsync(
        Guid vesselId,
        Guid? loadcaseId,
        List<string> curveTypes,
        decimal minDraft,
        decimal maxDraft,
        int points,
        CancellationToken cancellationToken)
    {
        var curves = new List<Shared.DTOs.CurveDto>();

        foreach (var type in curveTypes)
        {
            switch (type.ToLower())
            {
                case "displacement":
                    var dispCurve = await _curvesGenerator.GenerateDisplacementCurveAsync(
                        vesselId, loadcaseId, minDraft, maxDraft, points, cancellationToken);
                    curves.Add(dispCurve);
                    break;

                case "kb":
                    var kbCurve = await _curvesGenerator.GenerateKBCurveAsync(
                        vesselId, loadcaseId, minDraft, maxDraft, points, cancellationToken);
                    curves.Add(kbCurve);
                    break;

                case "lcb":
                    var lcbCurve = await _curvesGenerator.GenerateLCBCurveAsync(
                        vesselId, loadcaseId, minDraft, maxDraft, points, cancellationToken);
                    curves.Add(lcbCurve);
                    break;

                case "awp":
                    var awpCurve = await _curvesGenerator.GenerateAwpCurveAsync(
                        vesselId, loadcaseId, minDraft, maxDraft, points, cancellationToken);
                    curves.Add(awpCurve);
                    break;

                case "gmt":
                    var gmtCurve = await _curvesGenerator.GenerateGMtCurveAsync(
                        vesselId, loadcaseId, minDraft, maxDraft, points, cancellationToken);
                    curves.Add(gmtCurve);
                    break;
            }
        }

        return curves;
    }

}

/// <summary>
/// Request model for exporting hydrostatic table
/// </summary>
public record ExportTableRequest
{
    public Guid? LoadcaseId { get; init; }
    public List<decimal> Drafts { get; init; } = new();
}

/// <summary>
/// Request model for exporting curves
/// </summary>
public record ExportCurvesRequest
{
    public Guid? LoadcaseId { get; init; }
    public List<string> CurveTypes { get; init; } = new();
    public decimal MinDraft { get; init; }
    public decimal MaxDraft { get; init; }
    public int Points { get; init; } = 100;
}

/// <summary>
/// Request model for exporting comprehensive report
/// </summary>
public record ExportReportRequest
{
    public Guid? LoadcaseId { get; init; }
    public bool IncludeCurves { get; init; }
}

