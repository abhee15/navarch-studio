using ClosedXML.Excel;
using Shared.DTOs;
using Shared.Models;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Helper class to build professional Excel reports for hydrostatic analysis
/// </summary>
public class ExcelReportBuilder
{
    /// <summary>
    /// Generates a comprehensive hydrostatic report in Excel format
    /// </summary>
    public static byte[] GenerateReport(
        Vessel vessel,
        Loadcase? loadcase,
        List<HydroResultDto> results,
        List<CurveDto>? curves = null)
    {
        using var workbook = new XLWorkbook();

        // Sheet 1: Vessel Information
        CreateVesselInfoSheet(workbook, vessel, loadcase);

        // Sheet 2: Hydrostatic Table
        CreateHydrostaticTableSheet(workbook, results);

        // Sheet 3: Curves Data (if provided)
        if (curves != null && curves.Any())
        {
            CreateCurvesSheet(workbook, curves);
        }

        // Save to memory stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void CreateVesselInfoSheet(XLWorkbook workbook, Vessel vessel, Loadcase? loadcase)
    {
        var worksheet = workbook.Worksheets.Add("Vessel Information");

        // Title
        worksheet.Cell(1, 1).Value = "HYDROSTATIC ANALYSIS REPORT";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.DarkBlue;
        worksheet.Range(1, 1, 1, 2).Merge();

        // Generated timestamp
        worksheet.Cell(2, 1).Value = $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        worksheet.Cell(2, 1).Style.Font.Italic = true;
        worksheet.Cell(2, 1).Style.Font.FontSize = 9;
        worksheet.Range(2, 1, 2, 2).Merge();

        int row = 4;

        // Vessel Particulars Header
        worksheet.Cell(row, 1).Value = "VESSEL PARTICULARS";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Font.FontSize = 12;
        worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        worksheet.Range(row, 1, row, 2).Merge();
        row++;

        // Vessel data
        AddInfoRow(worksheet, ref row, "Vessel Name", vessel.Name);
        AddInfoRow(worksheet, ref row, "Description", vessel.Description ?? "N/A");
        AddInfoRow(worksheet, ref row, "Length (Lpp)", $"{vessel.Lpp:F2} m");
        AddInfoRow(worksheet, ref row, "Beam", $"{vessel.Beam:F2} m");
        AddInfoRow(worksheet, ref row, "Design Draft", $"{vessel.DesignDraft:F2} m");

        row++;

        // Loadcase information (if provided)
        if (loadcase != null)
        {
            worksheet.Cell(row, 1).Value = "LOADCASE INFORMATION";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Font.FontSize = 12;
            worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            worksheet.Range(row, 1, row, 2).Merge();
            row++;

            AddInfoRow(worksheet, ref row, "Loadcase Name", loadcase.Name);
            AddInfoRow(worksheet, ref row, "Water Density (ρ)", $"{loadcase.Rho:F1} kg/m³");
            if (loadcase.KG.HasValue)
                AddInfoRow(worksheet, ref row, "VCG (KG)", $"{loadcase.KG:F2} m");
            if (!string.IsNullOrEmpty(loadcase.Notes))
                AddInfoRow(worksheet, ref row, "Notes", loadcase.Notes);
        }

        // Format columns
        worksheet.Column(1).Width = 25;
        worksheet.Column(2).Width = 40;
    }

    private static void CreateHydrostaticTableSheet(XLWorkbook workbook, List<HydroResultDto> results)
    {
        var worksheet = workbook.Worksheets.Add("Hydrostatic Table");

        // Title
        worksheet.Cell(1, 1).Value = "HYDROSTATIC PROPERTIES";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, 12).Merge();

        // Headers
        int col = 1;
        int headerRow = 3;
        var headers = new[]
        {
            "Draft (m)",
            "Disp. Volume (m³)",
            "Disp. Weight (kg)",
            "KB (m)",
            "LCB (m)",
            "TCB (m)",
            "BMt (m)",
            "BMl (m)",
            "GMt (m)",
            "GMl (m)",
            "Awp (m²)",
            "Iwp (m⁴)",
            "Cb",
            "Cp",
            "Cm",
            "Cwp"
        };

        foreach (var header in headers)
        {
            var cell = worksheet.Cell(headerRow, col);
            cell.Value = header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            col++;
        }

        // Data rows
        int dataRow = headerRow + 1;
        foreach (var result in results)
        {
            col = 1;
            worksheet.Cell(dataRow, col++).Value = result.Draft;
            worksheet.Cell(dataRow, col++).Value = result.DispVolume;
            worksheet.Cell(dataRow, col++).Value = result.DispWeight;
            worksheet.Cell(dataRow, col++).Value = result.KBz;
            worksheet.Cell(dataRow, col++).Value = result.LCBx;
            worksheet.Cell(dataRow, col++).Value = result.TCBy;
            worksheet.Cell(dataRow, col++).Value = result.BMt;
            worksheet.Cell(dataRow, col++).Value = result.BMl;
            worksheet.Cell(dataRow, col++).Value = result.GMt ?? 0;
            worksheet.Cell(dataRow, col++).Value = result.GMl ?? 0;
            worksheet.Cell(dataRow, col++).Value = result.Awp;
            worksheet.Cell(dataRow, col++).Value = result.Iwp;
            worksheet.Cell(dataRow, col++).Value = result.Cb;
            worksheet.Cell(dataRow, col++).Value = result.Cp;
            worksheet.Cell(dataRow, col++).Value = result.Cm;
            worksheet.Cell(dataRow, col++).Value = result.Cwp;

            dataRow++;
        }

        // Format data range
        var dataRange = worksheet.Range(headerRow + 1, 1, dataRow - 1, headers.Length);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;

        // Format number columns
        for (int c = 1; c <= headers.Length; c++)
        {
            worksheet.Column(c).Width = 12;
            if (c <= 12) // Numeric columns
            {
                worksheet.Range(headerRow + 1, c, dataRow - 1, c).Style.NumberFormat.Format = "0.00";
            }
            else // Coefficient columns
            {
                worksheet.Range(headerRow + 1, c, dataRow - 1, c).Style.NumberFormat.Format = "0.0000";
            }
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Add note
        worksheet.Cell(dataRow + 2, 1).Value = "Note: All values are in SI units (meters, kilograms)";
        worksheet.Cell(dataRow + 2, 1).Style.Font.Italic = true;
        worksheet.Cell(dataRow + 2, 1).Style.Font.FontSize = 9;
        worksheet.Range(dataRow + 2, 1, dataRow + 2, 4).Merge();
    }

    private static void CreateCurvesSheet(XLWorkbook workbook, List<CurveDto> curves)
    {
        var worksheet = workbook.Worksheets.Add("Curves Data");

        // Title
        worksheet.Cell(1, 1).Value = "HYDROSTATIC CURVES DATA";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, 3).Merge();

        int row = 3;

        foreach (var curve in curves)
        {
            // Curve title
            worksheet.Cell(row, 1).Value = $"{curve.Type} Curve";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Range(row, 1, row, 2).Merge();
            row++;

            // Headers
            worksheet.Cell(row, 1).Value = curve.XLabel;
            worksheet.Cell(row, 2).Value = curve.YLabel;
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            worksheet.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightBlue;
            row++;

            // Data points
            foreach (var point in curve.Points)
            {
                worksheet.Cell(row, 1).Value = point.X;
                worksheet.Cell(row, 2).Value = point.Y;
                row++;
            }

            row += 2; // Add spacing between curves
        }

        // Format columns
        worksheet.Column(1).Width = 20;
        worksheet.Column(2).Width = 20;
        worksheet.Columns().AdjustToContents();
    }

    private static void AddInfoRow(IXLWorksheet worksheet, ref int row, string label, string value)
    {
        worksheet.Cell(row, 1).Value = label;
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 2).Value = value;
        row++;
    }
}

