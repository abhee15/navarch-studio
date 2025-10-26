using System.Text;
using System.Text.Json;
using Shared.DTOs;
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

    public Task<byte[]> ExportToPdfAsync(
        Guid vesselId,
        Guid? loadcaseId,
        bool includeCurves,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement PDF export using iText7 or QuestPDF
        // For MVP, throw NotImplementedException with guidance
        _logger.LogWarning("PDF export not yet implemented. Use CSV/JSON export for now.");

        throw new NotImplementedException(
            "PDF export is planned for a future release. " +
            "Please use CSV or JSON export formats for now. " +
            "Libraries like iText7, QuestPDF, or PdfSharp can be added later.");
    }

    public Task<byte[]> ExportToExcelAsync(
        Guid vesselId,
        Guid? loadcaseId,
        bool includeCurves,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement Excel export using EPPlus or ClosedXML
        // For MVP, throw NotImplementedException with guidance
        _logger.LogWarning("Excel export not yet implemented. Use CSV/JSON export for now.");

        throw new NotImplementedException(
            "Excel export is planned for a future release. " +
            "Please use CSV or JSON export formats for now. " +
            "Libraries like EPPlus or ClosedXML can be added later.");
    }
}

