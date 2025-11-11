using System.Text;
using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for exporting hull geometry to IGES 5.3 format
/// IGES (Initial Graphics Exchange Specification) is a vendor-neutral CAD file format
/// </summary>
public class IgesExportService : IIgesExportService
{
    private readonly DataDbContext _context;
    private readonly IDigonalsService _diagonalsService;
    private readonly ILogger<IgesExportService> _logger;

    public IgesExportService(
        DataDbContext context,
        IDigonalsService diagonalsService,
        ILogger<IgesExportService> logger)
    {
        _context = context;
        _diagonalsService = diagonalsService;
        _logger = logger;
    }

    /// <summary>
    /// Exports hull geometry as IGES 5.3 file
    /// </summary>
    public async Task<byte[]> ExportToIgesAsync(
        Guid vesselId,
        IgesExportRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating IGES export for vessel {VesselId}", vesselId);

        // Load vessel
        var vessel = await _context.Vessels
            .FirstOrDefaultAsync(v => v.Id == vesselId, cancellationToken);

        if (vessel == null)
        {
            throw new ArgumentException($"Vessel {vesselId} not found");
        }

        // Load geometry
        var stations = await _context.Stations
            .Where(s => s.VesselId == vesselId)
            .OrderBy(s => s.StationIndex)
            .ToListAsync(cancellationToken);

        var waterlines = await _context.Waterlines
            .Where(w => w.VesselId == vesselId)
            .OrderBy(w => w.WaterlineIndex)
            .ToListAsync(cancellationToken);

        var offsets = await _context.Offsets
            .Where(o => o.VesselId == vesselId)
            .ToListAsync(cancellationToken);

        if (stations.Count == 0 || waterlines.Count == 0 || offsets.Count == 0)
        {
            throw new ArgumentException($"No geometry data available for vessel {vesselId}");
        }

        // Load optional data
        DiagonalsDto? diagonals = null;
        if (request.IncludeDiagonals)
        {
            diagonals = await _diagonalsService.GetDiagonalsAsync(vesselId, 3, cancellationToken);
        }

        // Generate IGES file
        var igesContent = GenerateIgesFile(vessel, stations, waterlines, offsets, diagonals, request);

        _logger.LogInformation(
            "Generated IGES file for vessel {VesselId}: {Size} bytes",
            vesselId,
            igesContent.Length);

        return igesContent;
    }

    /// <summary>
    /// Generates IGES 5.3 file content
    /// </summary>
    private byte[] GenerateIgesFile(
        Shared.Models.Vessel vessel,
        List<Shared.Models.Station> stations,
        List<Shared.Models.Waterline> waterlines,
        List<Shared.Models.Offset> offsets,
        DiagonalsDto? diagonals,
        IgesExportRequest request)
    {
        var sb = new StringBuilder();

        // IGES file format:
        // S (Start) section - General information
        // G (Global) section - File parameters
        // D (Directory Entry) section - Entity directory
        // P (Parameter Data) section - Entity parameters
        // T (Terminate) section - File terminator

        int sequenceNumber = 1;

        // ========== START SECTION ==========
        sb.AppendLine($"NavArch Studio IGES Export - Vessel: {vessel.Name}                            S{sequenceNumber++,7}");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC                         S{sequenceNumber++,7}");

        // ========== GLOBAL SECTION ==========
        int globalSeq = 1;
        sb.AppendLine($"1H,,1H;,7HUnknown,11HNavArch.igs,11HNavArch 1.0,32,38,6,308,15,         G{globalSeq++,7}");
        sb.AppendLine($"7HUnknown,1.0,2,2HMM,1,0.01,15H{DateTime.UtcNow:yyyyMMddHHmmss},             G{globalSeq++,7}");
        sb.AppendLine($"1E-005,1000.0,7HUnknown,11HUnknown.igs,11;                                  G{globalSeq++,7}");

        // ========== DIRECTORY & PARAMETER SECTIONS ==========
        var directoryEntries = new List<string>();
        var parameterEntries = new List<string>();

        int entityNumber = 1;
        int parameterSeq = 1;

        // Export station curves (if requested)
        if (request.IncludeStations)
        {
            foreach (var station in stations)
            {
                var stationOffsets = offsets
                    .Where(o => o.StationIndex == station.StationIndex)
                    .OrderBy(o => o.WaterlineIndex)
                    .ToList();

                if (stationOffsets.Count >= 2)
                {
                    // Create IGES entity 126 (B-spline curve) for this station
                    AddBSplineCurve(
                        directoryEntries,
                        parameterEntries,
                        ref entityNumber,
                        ref parameterSeq,
                        stationOffsets,
                        waterlines,
                        $"Station_{station.StationIndex}");
                }
            }
        }

        // Build final IGES content
        var iges = new StringBuilder();

        // Start section
        foreach (var line in sb.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Where(l => l.Contains("S")))
        {
            iges.AppendLine(line);
        }

        // Global section
        foreach (var line in sb.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Where(l => l.Contains("G")))
        {
            iges.AppendLine(line);
        }

        // Directory section
        foreach (var line in directoryEntries)
        {
            iges.AppendLine(line);
        }

        // Parameter section
        foreach (var line in parameterEntries)
        {
            iges.AppendLine(line);
        }

        // Terminate section
        int startLines = sb.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Count(l => l.Contains("S"));
        int globalLines = sb.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Count(l => l.Contains("G"));

        iges.AppendLine($"S{startLines,7}G{globalLines,7}D{directoryEntries.Count,7}P{parameterEntries.Count,7}                                        T{1,7}");

        return Encoding.ASCII.GetBytes(iges.ToString());
    }

    /// <summary>
    /// Adds a B-spline curve entity to IGES file
    /// </summary>
    private void AddBSplineCurve(
        List<string> directoryEntries,
        List<string> parameterEntries,
        ref int entityNumber,
        ref int parameterSeq,
        List<Shared.Models.Offset> stationOffsets,
        List<Shared.Models.Waterline> waterlines,
        string label)
    {
        // Directory Entry (2 lines per entity)
        // Format: Entity type, Parameter pointer, Structure, Line font, Level, View, Transform, Label, Status, Line weight, Color, Form
        directoryEntries.Add($"     126{parameterSeq,7}       0       0       0       0       0       0       0D{entityNumber * 2 - 1,7}");
        directoryEntries.Add($"     126       0       0       1       0                               0D{entityNumber * 2,7}");

        // Parameter Data (simplified for IGES 5.3)
        // For a simple polyline approximation
        int numPoints = stationOffsets.Count;

        var paramBuilder = new StringBuilder();
        paramBuilder.Append("126,");  // Entity type
        paramBuilder.Append($"{numPoints},");  // Number of control points
        paramBuilder.Append("1,");  // Degree (linear)
        paramBuilder.Append("0,");  // Planar flag
        paramBuilder.Append("0,");  // Open curve
        paramBuilder.Append("0,");  // Polynomial
        paramBuilder.Append("0,");  // Periodic

        // Add control points (Z, Y coordinates for station curve)
        foreach (var offset in stationOffsets)
        {
            var waterline = waterlines.FirstOrDefault(w => w.WaterlineIndex == offset.WaterlineIndex);
            if (waterline != null)
            {
                paramBuilder.Append($"{waterline.Z:F6},{offset.HalfBreadthY:F6},0.0,");
            }
        }

        paramBuilder.Append(";");

        parameterEntries.Add(paramBuilder.ToString().PadRight(72) + $"P{parameterSeq,7}");

        entityNumber++;
        parameterSeq++;
    }
}
