using System.Text;
using System.Text.Json;
using Shared.DTOs;
using Shared.Models;
using DataService.Data;
using Microsoft.EntityFrameworkCore;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Implementation of export service for hydrostatic data
/// </summary>
public class ExportService : IExportService
{
    private readonly IHydroCalculator _hydroCalculator;
    private readonly ICurvesGenerator _curvesGenerator;
    private readonly DataDbContext _context;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        IHydroCalculator hydroCalculator,
        ICurvesGenerator curvesGenerator,
        DataDbContext context,
        ILogger<ExportService> logger)
    {
        _hydroCalculator = hydroCalculator;
        _curvesGenerator = curvesGenerator;
        _context = context;
        _logger = logger;
    }

    public Task<byte[]> ExportToCsvAsync(
        List<HydroResultDto> results,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var csv = new StringBuilder();

            // Header
            csv.AppendLine("Draft (m),Displacement (kg),Volume (m³),KB (m),LCB (m),TCB (m),BMt (m),BMl (m),GMt (m),GMl (m),Awp (m²),Iwp (m⁴),Cb,Cp,Cm,Cwp");

            // Data rows
            foreach (var result in results)
            {
                csv.AppendLine($"{result.Draft:F3},{result.DispWeight:F0},{result.DispVolume:F3},{result.KBz:F3},{result.LCBx:F3},{result.TCBy:F3},{result.BMt:F3},{result.BMl:F3},{result.GMt:F3},{result.GMl:F3},{result.Awp:F3},{result.Iwp:F3},{result.Cb:F4},{result.Cp:F4},{result.Cm:F4},{result.Cwp:F4}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }, cancellationToken);
    }

    public Task<byte[]> ExportToJsonAsync(
        List<HydroResultDto> results,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(results, options);
            return Encoding.UTF8.GetBytes(json);
        }, cancellationToken);
    }

    public Task<byte[]> ExportCurvesToCsvAsync(
        List<CurveDto> curves,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var csv = new StringBuilder();

            // For each curve, create a section
            foreach (var curve in curves)
            {
                csv.AppendLine($"# {curve.Type} Curve");
                csv.AppendLine($"{curve.XLabel},{curve.YLabel}");

                foreach (var point in curve.Points)
                {
                    csv.AppendLine($"{point.X:F3},{point.Y:F3}");
                }

                csv.AppendLine(); // Empty line between curves
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }, cancellationToken);
    }

    public Task<byte[]> ExportCurvesToJsonAsync(
        List<CurveDto> curves,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(curves, options);
            return Encoding.UTF8.GetBytes(json);
        }, cancellationToken);
    }

    public async Task<byte[]> ExportToPdfAsync(
        Guid vesselId,
        Guid? loadcaseId,
        bool includeCurves,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating PDF report for vessel {VesselId}", vesselId);

        // Load vessel
        var vessel = await _context.Vessels
            .FirstOrDefaultAsync(v => v.Id == vesselId, cancellationToken);

        if (vessel == null)
            throw new ArgumentException($"Vessel {vesselId} not found");

        // Load loadcase if specified
        Loadcase? loadcase = null;
        if (loadcaseId.HasValue)
        {
            loadcase = await _context.Loadcases
                .FirstOrDefaultAsync(lc => lc.Id == loadcaseId, cancellationToken);
        }

        // Generate hydrostatic table (use design draft range)
        var minDraft = vessel.DesignDraft * 0.3m;
        var maxDraft = vessel.DesignDraft * 1.2m;
        var draftStep = (maxDraft - minDraft) / 10;
        var drafts = new List<decimal>();
        for (var d = minDraft; d <= maxDraft; d += draftStep)
        {
            drafts.Add(Math.Round(d, 2));
        }

        var results = await _hydroCalculator.ComputeTableAsync(
            vesselId,
            loadcaseId,
            drafts,
            cancellationToken);

        // Generate curves if requested
        List<CurveDto>? curves = null;
        if (includeCurves)
        {
            try
            {
                var curveTypes = new[] { "displacement", "kb", "lcb", "awp", "gmt" };
                var curvesDict = await _curvesGenerator.GenerateMultipleCurvesAsync(
                    vesselId,
                    loadcaseId,
                    curveTypes.ToList(),
                    minDraft,
                    maxDraft,
                    100, // points
                    cancellationToken);
                curves = curvesDict.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate curves for PDF report");
                // Continue without curves
            }
        }

        // Generate PDF
        var pdfBytes = await Task.Run(() =>
            PdfReportBuilder.GenerateReport(vessel, loadcase, results, curves),
            cancellationToken);

        _logger.LogInformation("PDF report generated successfully for vessel {VesselId}, size: {Size} bytes",
            vesselId, pdfBytes.Length);

        return pdfBytes;
    }

    public async Task<byte[]> ExportToExcelAsync(
        Guid vesselId,
        Guid? loadcaseId,
        bool includeCurves,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Excel report for vessel {VesselId}", vesselId);

        // Load vessel
        var vessel = await _context.Vessels
            .FirstOrDefaultAsync(v => v.Id == vesselId, cancellationToken);

        if (vessel == null)
            throw new ArgumentException($"Vessel {vesselId} not found");

        // Load loadcase if specified
        Loadcase? loadcase = null;
        if (loadcaseId.HasValue)
        {
            loadcase = await _context.Loadcases
                .FirstOrDefaultAsync(lc => lc.Id == loadcaseId, cancellationToken);
        }

        // Generate hydrostatic table (use design draft range)
        var minDraft = vessel.DesignDraft * 0.3m;
        var maxDraft = vessel.DesignDraft * 1.2m;
        var draftStep = (maxDraft - minDraft) / 10;
        var drafts = new List<decimal>();
        for (var d = minDraft; d <= maxDraft; d += draftStep)
        {
            drafts.Add(Math.Round(d, 2));
        }

        var results = await _hydroCalculator.ComputeTableAsync(
            vesselId,
            loadcaseId,
            drafts,
            cancellationToken);

        // Generate curves if requested
        List<CurveDto>? curves = null;
        if (includeCurves)
        {
            try
            {
                var curveTypes = new[] { "displacement", "kb", "lcb", "awp", "gmt" };
                var curvesDict = await _curvesGenerator.GenerateMultipleCurvesAsync(
                    vesselId,
                    loadcaseId,
                    curveTypes.ToList(),
                    minDraft,
                    maxDraft,
                    100, // points
                    cancellationToken);
                curves = curvesDict.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate curves for Excel report");
                // Continue without curves
            }
        }

        // Generate Excel
        var excelBytes = await Task.Run(() =>
            ExcelReportBuilder.GenerateReport(vessel, loadcase, results, curves),
            cancellationToken);

        _logger.LogInformation("Excel report generated successfully for vessel {VesselId}, size: {Size} bytes",
            vesselId, excelBytes.Length);

        return excelBytes;
    }
}

