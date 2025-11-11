using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for exporting hull geometry to IGES format
/// </summary>
public interface IIgesExportService
{
    /// <summary>
    /// Exports hull geometry as IGES 5.3 file
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="request">Export configuration options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>IGES file content as byte array</returns>
    Task<byte[]> ExportToIgesAsync(
        Guid vesselId,
        IgesExportRequest request,
        CancellationToken cancellationToken);
}
