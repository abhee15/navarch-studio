namespace DataService.Services.Seakeeping;

/// <summary>
/// Export service for seakeeping analysis results.
/// </summary>
public interface ISeakeepingExportService
{
    /// <summary>
    /// Generate PDF report for RAO and motion response results.
    /// </summary>
    Task<byte[]> GeneratePdfReportAsync(
        Guid raoResultId,
        Guid? motionResponseId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Generate CSV export of RAO data.
    /// </summary>
    Task<byte[]> GenerateCsvAsync(
        Guid raoResultId,
        CancellationToken cancellationToken = default
    );
}
