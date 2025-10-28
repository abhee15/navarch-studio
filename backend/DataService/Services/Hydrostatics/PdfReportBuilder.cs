using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Shared.DTOs;
using Shared.Models;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Helper class to build professional PDF reports for hydrostatic analysis
/// </summary>
public class PdfReportBuilder
{
    /// <summary>
    /// Generates a comprehensive hydrostatic report PDF
    /// </summary>
    public static byte[] GenerateReport(
        Vessel vessel,
        Loadcase? loadcase,
        List<HydroResultDto> results,
        List<CurveDto>? curves = null,
        StabilityCurveDto? stabilityCurve = null,
        StabilityCriteriaResultDto? stabilityCriteria = null)
    {
        // Configure QuestPDF license (Community license for open source projects)
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header()
                    .Height(80)
                    .Background(Colors.Blue.Lighten3)
                    .Padding(10)
                    .Column(column =>
                    {
                        column.Item().Text("Hydrostatic Analysis Report")
                            .FontSize(20)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        column.Item().Text($"Vessel: {vessel.Name}")
                            .FontSize(14)
                            .SemiBold();

                        column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);
                    });

                page.Content()
                    .PaddingVertical(10)
                    .Column(column =>
                    {
                        column.Spacing(15);

                        // Vessel Information Section
                        column.Item().Element(c => RenderVesselInfo(c, vessel, loadcase));

                        // Inputs Summary Section
                        column.Item().Element(c => RenderInputsSummary(c, vessel, loadcase, results));

                        // Methodology Notes Section
                        column.Item().Element(c => RenderMethodologyNotes(c, vessel, results));

                        // Standards Reference Section
                        column.Item().Element(c => RenderStandardsReference(c));

                        // Hydrostatic Table Section
                        column.Item().Element(c => RenderHydrostaticTable(c, results));

                        // Form Coefficients Summary
                        if (results.Any())
                        {
                            column.Item().Element(c => RenderFormCoefficients(c, results));
                        }

                        // Curves Section (if provided)
                        if (curves != null && curves.Any())
                        {
                            column.Item().PageBreak();
                            column.Item().Element(c => RenderCurvesSection(c, curves));
                        }

                        // Stability Section (if provided)
                        if (stabilityCurve != null && stabilityCriteria != null)
                        {
                            column.Item().PageBreak();
                            column.Item().Element(c => RenderStabilitySection(c, stabilityCurve, stabilityCriteria));
                        }
                    });

                page.Footer()
                    .Height(30)
                    .Background(Colors.Grey.Lighten3)
                    .Padding(10)
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .Text("NavArch Studio - Hydrostatic Analysis")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken1);

                        row.RelativeItem()
                            .AlignRight()
                            .Text(x =>
                            {
                                x.Span("Page ").FontSize(8).FontColor(Colors.Grey.Darken1);
                                x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                                x.Span(" of ").FontSize(8).FontColor(Colors.Grey.Darken1);
                                x.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                    });
            });
        });

        return document.GeneratePdf();
    }

    private static void RenderVesselInfo(IContainer container, Vessel vessel, Loadcase? loadcase)
    {
        container.Column(column =>
        {
            column.Item().Text("Vessel Particulars")
                .FontSize(14)
                .Bold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(150);
                    columns.RelativeColumn();
                });

                table.Cell().Border(1).Background(Colors.Grey.Lighten2)
                    .Padding(5).Text("Property").Bold();
                table.Cell().Border(1).Background(Colors.Grey.Lighten2)
                    .Padding(5).Text("Value").Bold();

                AddTableRow(table, "Vessel Name", vessel.Name);
                AddTableRow(table, "Description", vessel.Description ?? "N/A");
                AddTableRow(table, "Length (Lpp)", $"{vessel.Lpp:F2} m");
                AddTableRow(table, "Beam", $"{vessel.Beam:F2} m");
                AddTableRow(table, "Design Draft", $"{vessel.DesignDraft:F2} m");

                if (loadcase != null)
                {
                    AddTableRow(table, "Loadcase", loadcase.Name);
                    AddTableRow(table, "Water Density (ρ)", $"{loadcase.Rho:F1} kg/m³");
                    if (loadcase.KG.HasValue)
                        AddTableRow(table, "VCG (KG)", $"{loadcase.KG:F2} m");
                }
            });
        });
    }

    private static void RenderHydrostaticTable(IContainer container, List<HydroResultDto> results)
    {
        container.Column(column =>
        {
            column.Item().Text("Hydrostatic Properties")
                .FontSize(14)
                .Bold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(5).Table(table =>
            {
                // Define columns
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(45);  // Draft
                    columns.ConstantColumn(60);  // Displacement
                    columns.ConstantColumn(45);  // KB
                    columns.ConstantColumn(45);  // LCB
                    columns.ConstantColumn(45);  // BMt
                    columns.ConstantColumn(45);  // GMt
                    columns.ConstantColumn(40);  // Cb
                    columns.ConstantColumn(40);  // Cp
                });

                // Header row
                table.Cell().Border(1).Background(Colors.Blue.Lighten3)
                    .Padding(3).Text("Draft\n(m)").FontSize(8).Bold();
                table.Cell().Border(1).Background(Colors.Blue.Lighten3)
                    .Padding(3).Text("Disp.\n(kg)").FontSize(8).Bold();
                table.Cell().Border(1).Background(Colors.Blue.Lighten3)
                    .Padding(3).Text("KB\n(m)").FontSize(8).Bold();
                table.Cell().Border(1).Background(Colors.Blue.Lighten3)
                    .Padding(3).Text("LCB\n(m)").FontSize(8).Bold();
                table.Cell().Border(1).Background(Colors.Blue.Lighten3)
                    .Padding(3).Text("BMt\n(m)").FontSize(8).Bold();
                table.Cell().Border(1).Background(Colors.Blue.Lighten3)
                    .Padding(3).Text("GMt\n(m)").FontSize(8).Bold();
                table.Cell().Border(1).Background(Colors.Blue.Lighten3)
                    .Padding(3).Text("Cb").FontSize(8).Bold();
                table.Cell().Border(1).Background(Colors.Blue.Lighten3)
                    .Padding(3).Text("Cp").FontSize(8).Bold();

                // Data rows
                foreach (var result in results)
                {
                    table.Cell().Border(1).Padding(3).Text($"{result.Draft:F2}").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text($"{result.DispWeight:N0}").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text($"{result.KBz:F2}").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text($"{result.LCBx:F2}").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text($"{result.BMt:F2}").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text($"{result.GMt?.ToString("F2") ?? "N/A"}").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text($"{result.Cb:F3}").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text($"{result.Cp:F3}").FontSize(8);
                }
            });

            // Legend
            column.Item().PaddingTop(5).Text("Note: All values are in SI units (meters, kilograms)")
                .FontSize(8)
                .Italic()
                .FontColor(Colors.Grey.Darken1);
        });
    }

    private static void RenderFormCoefficients(IContainer container, List<HydroResultDto> results)
    {
        var avgResult = results[results.Count / 2]; // Use middle draft as representative

        container.Column(column =>
        {
            column.Item().Text("Form Coefficients Summary")
                .FontSize(12)
                .Bold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Cb (Block): {avgResult.Cb:F4}").FontSize(9);
                    c.Item().Text($"Cp (Prismatic): {avgResult.Cp:F4}").FontSize(9);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Cm (Midship): {avgResult.Cm:F4}").FontSize(9);
                    c.Item().Text($"Cwp (Waterplane): {avgResult.Cwp:F4}").FontSize(9);
                });
            });
        });
    }

    private static void RenderCurvesSection(IContainer container, List<CurveDto> curves)
    {
        container.Column(column =>
        {
            column.Item().Text("Hydrostatic Curves")
                .FontSize(14)
                .Bold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(5).Text("Curve data included in this report:")
                .FontSize(10);

            foreach (var curve in curves)
            {
                column.Item().PaddingTop(10).Column(c =>
                {
                    c.Item().Text($"• {curve.Type}")
                        .FontSize(10)
                        .Bold();
                    c.Item().Text($"  X-axis: {curve.XLabel}, Y-axis: {curve.YLabel}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                    c.Item().Text($"  Data points: {curve.Points.Count}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                });
            }

            column.Item().PaddingTop(10).Text("Note: For detailed curve visualizations, please refer to the web interface or export curves separately.")
                .FontSize(8)
                .Italic()
                .FontColor(Colors.Grey.Darken1);
        });
    }

    private static void AddTableRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Border(1).Padding(5).Text(label).FontSize(9);
        table.Cell().Border(1).Padding(5).Text(value).FontSize(9);
    }

    private static void RenderInputsSummary(IContainer container, Vessel vessel, Loadcase? loadcase, List<HydroResultDto> results)
    {
        container.Column(column =>
        {
            column.Item().Text("Inputs Summary")
                .FontSize(12)
                .Bold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                });

                table.Cell().Border(1).Padding(5).Text("Parameter").FontSize(9).Bold();
                table.Cell().Border(1).Padding(5).Text("Value").FontSize(9).Bold();

                AddTableRow(table, "Principal Dimensions", "");
                AddTableRow(table, "Length (Lpp)", $"{vessel.Lpp:F2} m");
                AddTableRow(table, "Beam", $"{vessel.Beam:F2} m");
                AddTableRow(table, "Design Draft", $"{vessel.DesignDraft:F2} m");

                if (loadcase != null)
                {
                    AddTableRow(table, "Loadcase Parameters", "");
                    AddTableRow(table, "Density (ρ)", $"{loadcase.Rho:F2} kg/m³");
                    if (loadcase.KG.HasValue)
                    {
                        AddTableRow(table, "Center of Gravity (KG)", $"{loadcase.KG.Value:F3} m");
                    }
                }

                AddTableRow(table, "Computation Range", "");
                AddTableRow(table, "Draft Range", $"{results.Min(r => r.Draft):F2} - {results.Max(r => r.Draft):F2} m");
                AddTableRow(table, "Number of Drafts", $"{results.Count}");
            });
        });
    }

    private static void RenderMethodologyNotes(IContainer container, Vessel vessel, List<HydroResultDto> results)
    {
        container.Column(column =>
        {
            column.Item().Text("Methodology Notes")
                .FontSize(12)
                .Bold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(5).Column(c =>
            {
                c.Item().Text("Integration Method:")
                    .FontSize(9)
                    .Bold();
                c.Item().PaddingLeft(10).Text("Simpson's Rule with trapezoidal fallback for non-uniform spacing")
                    .FontSize(9);

                c.Item().PaddingTop(5).Text("Symmetry Assumptions:")
                    .FontSize(9)
                    .Bold();
                c.Item().PaddingLeft(10).Text("Port/starboard symmetry assumed (TCB = 0)")
                    .FontSize(9);

                c.Item().PaddingTop(5).Text("Coordinate System:")
                    .FontSize(9)
                    .Bold();
                c.Item().PaddingLeft(10).Text("X: Longitudinal (forward positive), Y: Transverse (starboard positive), Z: Vertical (up positive)")
                    .FontSize(9);

                c.Item().PaddingTop(5).Text("Units:")
                    .FontSize(9)
                    .Bold();
                c.Item().PaddingLeft(10).Text("All values in SI units (meters, kilograms, cubic meters)")
                    .FontSize(9);
            });
        });
    }

    private static void RenderStandardsReference(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("Standards Reference")
                .FontSize(12)
                .Bold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(5).Column(c =>
            {
                c.Item().Text("Calculations follow recognized naval architecture standards:")
                    .FontSize(9);

                c.Item().PaddingTop(3).PaddingLeft(10).Text("• IMO MSC.267(85) - International Code on Intact Stability (terminology)")
                    .FontSize(8);

                c.Item().PaddingLeft(10).Text("• IMO A.749(18) - Code on Intact Stability for All Types of Ships (criteria)")
                    .FontSize(8);

                c.Item().PaddingLeft(10).Text("• ISO 12217 - Small Craft Stability (methodology reference)")
                    .FontSize(8);

                c.Item().PaddingTop(5).Text("Note: These references are informative. Users should consult applicable classification society rules and national regulations for specific requirements.")
                    .FontSize(7)
                    .Italic()
                    .FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private static void RenderStabilitySection(IContainer container, StabilityCurveDto stabilityCurve, StabilityCriteriaResultDto stabilityCriteria)
    {
        container.Column(column =>
        {
            column.Item().Text("Stability Analysis")
                .FontSize(14)
                .Bold()
                .FontColor(Colors.Blue.Darken1);

            // Key Metrics
            column.Item().PaddingTop(10).Text("Key Stability Parameters")
                .FontSize(12)
                .SemiBold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Initial GMT: {stabilityCurve.InitialGMT:F3} m").FontSize(9);
                    c.Item().Text($"Method: {stabilityCurve.Method}").FontSize(9);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Max GZ: {stabilityCurve.MaxGZ:F3} m").FontSize(9);
                    c.Item().Text($"Angle at Max GZ: {stabilityCurve.AngleAtMaxGZ:F1}°").FontSize(9);
                });
            });

            // Criteria Checklist
            column.Item().PaddingTop(15).Text("IMO A.749(18) Intact Stability Criteria")
                .FontSize(12)
                .SemiBold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(5).Text($"Overall Status: {(stabilityCriteria.AllCriteriaPassed ? "PASS" : "FAIL")}")
                .FontSize(10)
                .Bold()
                .FontColor(stabilityCriteria.AllCriteriaPassed ? Colors.Green.Darken2 : Colors.Red.Darken2);

            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                table.Cell().Border(1).Padding(5).Text("Criterion").FontSize(8).Bold();
                table.Cell().Border(1).Padding(5).Text("Required").FontSize(8).Bold();
                table.Cell().Border(1).Padding(5).Text("Actual").FontSize(8).Bold();
                table.Cell().Border(1).Padding(5).Text("Status").FontSize(8).Bold();

                foreach (var criterion in stabilityCriteria.Criteria)
                {
                    table.Cell().Border(1).Padding(5).Text(criterion.Name).FontSize(8);
                    table.Cell().Border(1).Padding(5).Text($"{criterion.RequiredValue:F3}").FontSize(8);
                    table.Cell().Border(1).Padding(5).Text($"{criterion.ActualValue:F3}").FontSize(8);
                    table.Cell().Border(1).Padding(5).Text(criterion.Passed ? "✓ Pass" : "✗ Fail")
                        .FontSize(8)
                        .FontColor(criterion.Passed ? Colors.Green.Darken2 : Colors.Red.Darken2);
                }
            });

            column.Item().PaddingTop(10).Text($"Summary: {stabilityCriteria.Summary}")
                .FontSize(9)
                .Italic()
                .FontColor(Colors.Grey.Darken1);

            // GZ Curve Data Summary
            column.Item().PaddingTop(15).Text("GZ Curve Data")
                .FontSize(12)
                .SemiBold()
                .FontColor(Colors.Blue.Darken1);

            column.Item().PaddingTop(5).Text($"Angle Range: {stabilityCurve.Points.Min(p => p.HeelAngle):F0}° to {stabilityCurve.Points.Max(p => p.HeelAngle):F0}°")
                .FontSize(9);
            column.Item().Text($"Number of Points: {stabilityCurve.Points.Count}")
                .FontSize(9);
            column.Item().Text($"Computation Time: {stabilityCurve.ComputationTimeMs} ms")
                .FontSize(9);
        });
    }
}

