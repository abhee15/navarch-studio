namespace Shared.DTOs;

/// <summary>
/// Request for exporting lines plan as PDF
/// </summary>
public class LinesPlanExportRequest : UnitAwareDto
{
    /// <summary>
    /// Paper size for PDF export
    /// </summary>
    public string PaperSize { get; set; } = "A1"; // A0, A1, A2, A3, Letter, Tabloid

    /// <summary>
    /// Scale for the lines plan
    /// </summary>
    public string Scale { get; set; } = "1:100"; // 1:50, 1:100, 1:200, 1:500, Custom

    /// <summary>
    /// Page orientation
    /// </summary>
    public string Orientation { get; set; } = "Landscape"; // Landscape, Portrait

    /// <summary>
    /// Include title block with vessel information
    /// </summary>
    public bool IncludeTitleBlock { get; set; } = true;

    /// <summary>
    /// Include grid lines
    /// </summary>
    public bool IncludeGrid { get; set; } = true;

    /// <summary>
    /// Include offsets reference table
    /// </summary>
    public bool IncludeOffsetsTable { get; set; } = true;

    /// <summary>
    /// Include section area curve overlay
    /// </summary>
    public bool IncludeSectionAreaCurve { get; set; } = true;

    /// <summary>
    /// Include diagonal curves
    /// </summary>
    public bool IncludeDiagonals { get; set; } = true;

    /// <summary>
    /// Export quality (Draft = low res/fast, Final = high res/slower)
    /// </summary>
    public string Quality { get; set; } = "Final"; // Draft, Final

    /// <summary>
    /// Color mode (true = color, false = grayscale)
    /// </summary>
    public bool ColorMode { get; set; } = true;

    /// <summary>
    /// Optional watermark text
    /// </summary>
    public string Watermark { get; set; } = ""; // e.g., "DRAFT", "CONFIDENTIAL"
}
