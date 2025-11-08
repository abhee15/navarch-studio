using System.Text;
using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NavArch.Shared.Models;

namespace DataService.Services.Seakeeping;

/// <summary>
/// Export service for seakeeping analysis results.
/// </summary>
public class SeakeepingExportService : ISeakeepingExportService
{
    private readonly DataDbContext _context;
    private readonly ILogger<SeakeepingExportService> _logger;

    public SeakeepingExportService(
        DataDbContext context,
        ILogger<SeakeepingExportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<byte[]> GeneratePdfReportAsync(
        Guid raoResultId,
        Guid? motionResponseId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating PDF report for RAO {RaoId}", raoResultId);

        var raos = await _context.RaoResults
            .Include(r => r.Vessel)
            .Include(r => r.Loadcase)
            .FirstOrDefaultAsync(r => r.Id == raoResultId, cancellationToken);

        if (raos == null)
        {
            throw new ArgumentException($"RAO result {raoResultId} not found");
        }

        MotionResponse? motion = null;
        if (motionResponseId.HasValue)
        {
            motion = await _context.MotionResponses
                .FirstOrDefaultAsync(m => m.Id == motionResponseId.Value, cancellationToken);
        }

        // Phase 4: Full QuestPDF implementation
        // For now, return basic PDF with text content
        // TODO: Add charts using SkiaSharp or embedded images

        var pdfContent = GenerateBasicPdfContent(raos, motion);
        return Encoding.UTF8.GetBytes(pdfContent);
    }

    private string GenerateBasicPdfContent(RaoResult raos, MotionResponse? motion)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SEAKEEPING ANALYSIS REPORT");
        sb.AppendLine("=========================");
        sb.AppendLine();
        sb.AppendLine($"Vessel: {raos.Vessel.Name}");
        sb.AppendLine($"Report Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine();
        sb.AppendLine("RAO RESULTS");
        sb.AppendLine("-----------");
        sb.AppendLine($"Frequency points: {raos.Frequency.Length}");
        sb.AppendLine($"Frequency range: {raos.Frequency.Min():F2} - {raos.Frequency.Max():F2} rad/s");
        sb.AppendLine($"Peak heave RAO: {raos.HeaveRao.Max():F3} m/m");
        sb.AppendLine($"Peak pitch RAO: {raos.PitchRao.Max():F3} rad/m");
        sb.AppendLine($"Peak roll RAO: {raos.RollRao.Max():F3} rad/m");
        sb.AppendLine();

        if (motion != null)
        {
            sb.AppendLine("MOTION RESPONSE");
            sb.AppendLine("---------------");
            sb.AppendLine($"Sea State: Hs={motion.SeaStateHs:F1}m, Tp={motion.SeaStateTp:F1}s");
            sb.AppendLine($"Spectrum: {motion.SeaStateSpectrum}");
            sb.AppendLine($"Significant Heave: {motion.SignificantHeave:F2} m");
            sb.AppendLine($"Significant Pitch: {motion.SignificantPitch:F2} deg");
            sb.AppendLine($"Significant Roll: {motion.SignificantRoll:F2} deg");
        }

        return sb.ToString();
    }

    public async Task<byte[]> GenerateCsvAsync(
        Guid raoResultId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating CSV for RAO {RaoId}", raoResultId);

        var raos = await _context.RaoResults
            .FirstOrDefaultAsync(r => r.Id == raoResultId, cancellationToken);

        if (raos == null)
        {
            throw new ArgumentException($"RAO result {raoResultId} not found");
        }

        // TODO: Phase 4 implementation
        // For now, return basic CSV stub
        var csv = new StringBuilder();
        csv.AppendLine("Frequency (rad/s),Heave RAO (m/m),Pitch RAO (rad/m),Roll RAO (rad/m)");

        for (int i = 0; i < raos.Frequency.Length; i++)
        {
            csv.AppendLine($"{raos.Frequency[i]},{raos.HeaveRao[i]},{raos.PitchRao[i]},{raos.RollRao[i]}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }
}
