using DataService.Services.Seakeeping;
using Microsoft.AspNetCore.Mvc;
using NavArch.Shared.DTOs;

namespace DataService.Controllers;

[ApiController]
[Route("api/v1/hydrostatics/vessels/{vesselId}/seakeeping")]
public class SeakeepingController : ControllerBase
{
    private readonly IRaoCalculator _raoCalculator;
    private readonly IMotionAnalysisService _motionAnalysis;
    private readonly ISeakeepingExportService _exportService;
    private readonly ILogger<SeakeepingController> _logger;

    public SeakeepingController(
        IRaoCalculator raoCalculator,
        IMotionAnalysisService motionAnalysis,
        ISeakeepingExportService exportService,
        ILogger<SeakeepingController> logger)
    {
        _raoCalculator = raoCalculator;
        _motionAnalysis = motionAnalysis;
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Calculate RAOs (Response Amplitude Operators) for a vessel.
    /// </summary>
    [HttpPost("raos")]
    [ProducesResponseType(typeof(RaoResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RaoResultDto>> CalculateRaos(
        [FromRoute] Guid vesselId,
        [FromBody] RaoCalculationRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "RAO calculation requested for vessel {VesselId}",
                vesselId
            );

            var result = await _raoCalculator.CalculateRaosAsync(
                vesselId,
                request,
                cancellationToken
            );

            return CreatedAtAction(
                nameof(GetRaos),
                new { vesselId, raoId = result.RaoId },
                result
            );
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error calculating RAOs");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation calculating RAOs");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get RAO results by ID.
    /// </summary>
    [HttpGet("raos/{raoId}")]
    [ProducesResponseType(typeof(RaoResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RaoResultDto>> GetRaos(
        [FromRoute] Guid vesselId,
        [FromRoute] Guid raoId,
        CancellationToken cancellationToken)
    {
        var result = await _raoCalculator.GetRaoByIdAsync(raoId, cancellationToken);

        if (result == null)
        {
            return NotFound(new { error = $"RAO result {raoId} not found" });
        }

        if (result.VesselId != vesselId)
        {
            return BadRequest(new { error = "RAO result does not belong to specified vessel" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Analyze motion response in irregular seas.
    /// </summary>
    [HttpPost("raos/{raoId}/motion-response")]
    [ProducesResponseType(typeof(MotionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MotionResponseDto>> AnalyzeMotion(
        [FromRoute] Guid vesselId,
        [FromRoute] Guid raoId,
        [FromBody] SeaStateDto seaState,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _motionAnalysis.AnalyzeMotionAsync(
                raoId,
                seaState,
                cancellationToken
            );

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Export RAO results to PDF or CSV.
    /// </summary>
    [HttpPost("raos/{raoId}/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportRaos(
        [FromRoute] Guid vesselId,
        [FromRoute] Guid raoId,
        [FromBody] ExportRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            byte[] fileData;
            string contentType;
            string fileName;

            switch (request.Format?.ToLower())
            {
                case "pdf":
                    fileData = await _exportService.GeneratePdfReportAsync(
                        raoId,
                        request.MotionResponseId,
                        cancellationToken
                    );
                    contentType = "application/pdf";
                    fileName = $"RAO_Report_{vesselId}.pdf";
                    break;

                case "csv":
                    fileData = await _exportService.GenerateCsvAsync(raoId, cancellationToken);
                    contentType = "text/csv";
                    fileName = $"RAO_Data_{vesselId}.csv";
                    break;

                default:
                    return BadRequest(new { error = "Invalid format. Use 'pdf' or 'csv'" });
            }

            return File(fileData, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record ExportRequestDto(
    string? Format,
    Guid? MotionResponseId
);

