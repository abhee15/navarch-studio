using DataService.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for generating professional lines plan PDFs using QuestPDF
/// </summary>
public class LinesPlanPdfService : ILinesPlanPdfService
{
    private readonly DataDbContext _context;
    private readonly IDigonalsService _diagonalsService;
    private readonly ISectionAreaCurveService _sectionAreaCurveService;
    private readonly ILogger<LinesPlanPdfService> _logger;

    public LinesPlanPdfService(
        DataDbContext context,
        IDigonalsService diagonalsService,
        ISectionAreaCurveService sectionAreaCurveService,
        ILogger<LinesPlanPdfService> logger)
    {
        _context = context;
        _diagonalsService = diagonalsService;
        _sectionAreaCurveService = sectionAreaCurveService;
        _logger = logger;
    }

    /// <summary>
    /// Generates a lines plan PDF with traditional 3-view layout
    /// </summary>
    public async Task<byte[]> GenerateLinesPlanPdfAsync(
        Guid vesselId,
        LinesPlanExportRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Generating lines plan PDF for vessel {VesselId} - Paper: {Paper}, Scale: {Scale}, Quality: {Quality}",
            vesselId,
            request.PaperSize,
            request.Scale,
            request.Quality);

        // Load vessel data
        var vessel = await _context.Vessels
            .FirstOrDefaultAsync(v => v.Id == vesselId, cancellationToken);

        if (vessel == null)
        {
            throw new ArgumentException($"Vessel {vesselId} not found");
        }

        // Load geometry data
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

        // Load optional data based on request
        DiagonalsDto? diagonals = null;
        SectionAreaCurveDto? sac = null;

        if (request.IncludeDiagonals)
        {
            diagonals = await _diagonalsService.GetDiagonalsAsync(vesselId, 3, cancellationToken);
        }

        if (request.IncludeSectionAreaCurve)
        {
            sac = await _sectionAreaCurveService.GetSectionAreaCurveAsync(vesselId, cancellationToken);
        }

        // Generate PDF using QuestPDF
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Configure page size
                ConfigurePageSize(page, request.PaperSize, request.Orientation);

                // Margins
                page.Margin(20);

                // Header (Title Block)
                if (request.IncludeTitleBlock)
                {
                    page.Header().Element(c => RenderTitleBlock(c, vessel, request));
                }

                // Content (Three-view layout)
                page.Content().Element(c => RenderLinesPlan(
                    c,
                    vessel,
                    stations,
                    waterlines,
                    offsets,
                    diagonals,
                    sac,
                    request));

                // Footer
                page.Footer().AlignCenter().Text($"NavArch Studio - Lines Plan Export | Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                    .FontSize(8).FontColor(Colors.Grey.Medium);

                // Optional watermark
                if (!string.IsNullOrWhiteSpace(request.Watermark))
                {
                    page.Foreground().AlignCenter().AlignMiddle().Rotate(-45).Text(request.Watermark)
                        .FontSize(72)
                        .FontColor(Colors.Grey.Lighten3);
                }
            });
        });

        var pdfBytes = document.GeneratePdf();

        _logger.LogInformation(
            "Generated lines plan PDF for vessel {VesselId}: {Size} bytes",
            vesselId,
            pdfBytes.Length);

        return pdfBytes;
    }

    /// <summary>
    /// Configure page size and orientation
    /// </summary>
    private void ConfigurePageSize(PageDescriptor page, string paperSize, string orientation)
    {
        var pageSize = paperSize switch
        {
            "A0" => PageSizes.A0,
            "A1" => PageSizes.A1,
            "A2" => PageSizes.A2,
            "A3" => PageSizes.A3,
            "Letter" => PageSizes.Letter,
            "Tabloid" => PageSizes.Tabloid,
            _ => PageSizes.A1
        };

        page.Size(pageSize);

        if (orientation == "Portrait")
        {
            page.PageColor(Colors.White);
        }
        else
        {
            page.PageColor(Colors.White);
        }
    }

    /// <summary>
    /// Render title block with vessel information
    /// </summary>
    private void RenderTitleBlock(
        IContainer container,
        Shared.Models.Vessel vessel,
        LinesPlanExportRequest request)
    {
        container.Border(1).BorderColor(Colors.Black).Padding(10).Column(column =>
        {
            column.Spacing(5);

            // Vessel name
            column.Item().Text(vessel.Name).FontSize(18).Bold();

            // Subtitle
            column.Item().Text("LINES PLAN - HULL FORM DOCUMENTATION").FontSize(12);

            // Principal Particulars
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Principal Particulars").FontSize(10).Bold();
                    col.Item().Text($"Lpp: {vessel.Lpp:F2} m").FontSize(9);
                    col.Item().Text($"Beam: {vessel.Beam:F2} m").FontSize(9);
                    col.Item().Text($"Draft: {vessel.DesignDraft:F2} m").FontSize(9);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Document Info").FontSize(10).Bold();
                    col.Item().Text($"Date: {DateTime.Now:yyyy-MM-dd}").FontSize(9);
                    col.Item().Text($"Scale: {request.Scale}").FontSize(9);
                    col.Item().Text($"Quality: {request.Quality}").FontSize(9);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Approval").FontSize(10).Bold();
                    col.Item().Text("Draft: ___________").FontSize(8);
                    col.Item().Text("Reviewed: ___________").FontSize(8);
                    col.Item().Text("Approved: ___________").FontSize(8);
                });
            });

            // Standards note
            column.Item().AlignRight().Text("Per IMO MSC.267(85) - NavArch Studio v1.0")
                .FontSize(7).Italic().FontColor(Colors.Grey.Medium);
        });
    }

    /// <summary>
    /// Render the three-view lines plan layout
    /// </summary>
    private void RenderLinesPlan(
        IContainer container,
        Shared.Models.Vessel vessel,
        List<Shared.Models.Station> stations,
        List<Shared.Models.Waterline> waterlines,
        List<Shared.Models.Offset> offsets,
        DiagonalsDto? diagonals,
        SectionAreaCurveDto? sac,
        LinesPlanExportRequest request)
    {
        container.Padding(10).Column(column =>
        {
            column.Spacing(20);

            // Top row: Body Plan (left) and Profile (right)
            column.Item().Row(row =>
            {
                row.Spacing(20);

                // Body Plan (left 40%)
                row.RelativeItem(4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10)
                    .Column(col =>
                    {
                        col.Item().Text("BODY PLAN").FontSize(10).Bold().AlignCenter();
                        col.Item().PaddingTop(5).Height(200)
                            .Element(c => RenderBodyPlan(c, stations, waterlines, offsets, request));
                    });

                // Profile Plan (right 60%)
                row.RelativeItem(6).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10)
                    .Column(col =>
                    {
                        col.Item().Text("PROFILE / SHEER PLAN").FontSize(10).Bold().AlignCenter();
                        col.Item().PaddingTop(5).Height(200)
                            .Element(c => RenderProfile(c, stations, waterlines, offsets, sac, request));
                    });
            });

            // Bottom row: Offsets Table (left) and Half-Breadth (right)
            column.Item().Row(row =>
            {
                row.Spacing(20);

                // Offsets Table (left 40%)
                if (request.IncludeOffsetsTable)
                {
                    row.RelativeItem(4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10)
                        .Column(col =>
                        {
                            col.Item().Text("OFFSETS REFERENCE").FontSize(10).Bold().AlignCenter();
                            col.Item().PaddingTop(5).Element(c => RenderOffsetsTable(c, stations, waterlines));
                        });
                }
                else
                {
                    row.RelativeItem(4).Text("");
                }

                // Half-Breadth Plan (right 60%)
                row.RelativeItem(6).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10)
                    .Column(col =>
                    {
                        col.Item().Text("HALF-BREADTH PLAN").FontSize(10).Bold().AlignCenter();
                        col.Item().PaddingTop(5).Height(200)
                            .Element(c => RenderHalfBreadth(c, stations, waterlines, offsets, diagonals, request));
                    });
            });
        });
    }

    /// <summary>
    /// Render body plan view (cross-sections) - Placeholder
    /// </summary>
    private void RenderBodyPlan(
        IContainer container,
        List<Shared.Models.Station> stations,
        List<Shared.Models.Waterline> waterlines,
        List<Shared.Models.Offset> offsets,
        LinesPlanExportRequest request)
    {
        // Simple placeholder text rendering
        // Full implementation would draw SVG/canvas-based station curves
        container.Background(Colors.Grey.Lighten3).Padding(10).Column(col =>
        {
            col.Item().Text("Body Plan Rendering").FontSize(12);
            col.Item().Text($"{stations.Count} stations × {waterlines.Count} waterlines").FontSize(10);
        });
    }

    /// <summary>
    /// Render profile view (side elevation) - Placeholder
    /// </summary>
    private void RenderProfile(
        IContainer container,
        List<Shared.Models.Station> stations,
        List<Shared.Models.Waterline> waterlines,
        List<Shared.Models.Offset> offsets,
        SectionAreaCurveDto? sac,
        LinesPlanExportRequest request)
    {
        // Simple placeholder text rendering
        container.Background(Colors.Grey.Lighten3).Padding(10).Column(col =>
        {
            col.Item().Text("Profile Plan Rendering").FontSize(12);

            if (request.IncludeSectionAreaCurve && sac != null)
            {
                col.Item().Text($"SAC: {sac.SectionalAreas.Count} points").FontSize(10).FontColor(Colors.Orange.Medium);
            }
        });
    }

    /// <summary>
    /// Render half-breadth view (top view) - Placeholder
    /// </summary>
    private void RenderHalfBreadth(
        IContainer container,
        List<Shared.Models.Station> stations,
        List<Shared.Models.Waterline> waterlines,
        List<Shared.Models.Offset> offsets,
        DiagonalsDto? diagonals,
        LinesPlanExportRequest request)
    {
        // Simple placeholder text rendering
        container.Background(Colors.Grey.Lighten3).Padding(10).Column(col =>
        {
            col.Item().Text("Half-Breadth Plan Rendering").FontSize(12);

            if (request.IncludeDiagonals && diagonals != null)
            {
                col.Item().Text($"Diagonals: {diagonals.Diagonals.Count}").FontSize(10).FontColor(Colors.Blue.Medium);
            }
        });
    }

    /// <summary>
    /// Render offsets reference table
    /// </summary>
    private void RenderOffsetsTable(
        IContainer container,
        List<Shared.Models.Station> stations,
        List<Shared.Models.Waterline> waterlines)
    {
        container.Column(column =>
        {
            column.Spacing(5);

            // Table with two columns: Stations and Waterlines
            column.Item().Row(row =>
            {
                row.Spacing(10);

                // Stations column
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Stations (X)").FontSize(9).Bold();

                    foreach (var station in stations.Take(10))
                    {
                        col.Item().Text($"{station.StationIndex}: {station.X:F2} m").FontSize(7);
                    }

                    if (stations.Count > 10)
                    {
                        col.Item().Text($"... ({stations.Count} total)").FontSize(7).Italic().FontColor(Colors.Grey.Medium);
                    }
                });

                // Waterlines column
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Waterlines (Z)").FontSize(9).Bold();

                    foreach (var waterline in waterlines.Take(10))
                    {
                        col.Item().Text($"{waterline.WaterlineIndex}: {waterline.Z:F2} m").FontSize(7);
                    }

                    if (waterlines.Count > 10)
                    {
                        col.Item().Text($"... ({waterlines.Count} total)").FontSize(7).Italic().FontColor(Colors.Grey.Medium);
                    }
                });
            });
        });
    }
}
