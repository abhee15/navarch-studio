using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for generating lines plan PDF exports
/// </summary>
public interface ILinesPlanPdfService
{
    /// <summary>
    /// Generates a professional lines plan PDF with traditional 3-view layout
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="request">Export configuration options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> GenerateLinesPlanPdfAsync(
        Guid vesselId,
        LinesPlanExportRequest request,
        CancellationToken cancellationToken);
}
