using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Interface for export service
/// Exports hydrostatic data to various formats (PDF, Excel, CSV, JSON)
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports hydrostatic table to CSV format
    /// </summary>
    /// <param name="results">Hydrostatic results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>CSV content as byte array</returns>
    Task<byte[]> ExportToCsvAsync(
        List<HydroResultDto> results,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports hydrostatic table to JSON format
    /// </summary>
    /// <param name="results">Hydrostatic results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON content as byte array</returns>
    Task<byte[]> ExportToJsonAsync(
        List<HydroResultDto> results,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports curve data to CSV format
    /// </summary>
    /// <param name="curves">Curve data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>CSV content as byte array</returns>
    Task<byte[]> ExportCurvesToCsvAsync(
        List<CurveDto> curves,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports curve data to JSON format
    /// </summary>
    /// <param name="curves">Curve data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON content as byte array</returns>
    Task<byte[]> ExportCurvesToJsonAsync(
        List<CurveDto> curves,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports comprehensive hydrostatics report to PDF
    /// Includes vessel details, hydrostatic table, and curves
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="loadcaseId">Loadcase ID (optional)</param>
    /// <param name="includeCurves">Include curves in report</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF content as byte array</returns>
    Task<byte[]> ExportToPdfAsync(
        Guid vesselId,
        Guid? loadcaseId,
        bool includeCurves,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports comprehensive hydrostatics report to Excel
    /// Includes multiple sheets for vessel, hydrostatics, curves
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="loadcaseId">Loadcase ID (optional)</param>
    /// <param name="includeCurves">Include curves in report</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Excel content as byte array</returns>
    Task<byte[]> ExportToExcelAsync(
        Guid vesselId,
        Guid? loadcaseId,
        bool includeCurves,
        CancellationToken cancellationToken = default);
}

