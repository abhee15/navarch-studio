using System.Security.Claims;
using DataService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace DataService.Controllers;

/// <summary>
/// API endpoints for comparison mode - comparing hydrostatic runs side-by-side
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComparisonController : ControllerBase
{
    private readonly ComparisonService _comparisonService;
    private readonly ILogger<ComparisonController> _logger;

    public ComparisonController(
        ComparisonService comparisonService,
        ILogger<ComparisonController> logger)
    {
        _comparisonService = comparisonService;
        _logger = logger;
    }

    /// <summary>
    /// Create a comparison snapshot for a vessel
    /// </summary>
    [HttpPost("vessels/{vesselId}/snapshots")]
    public async Task<ActionResult<ComparisonSnapshotDto>> CreateSnapshot(
        Guid vesselId,
        [FromBody] CreateComparisonSnapshotDto dto,
        [FromQuery] string displayUnits = "SI",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            var snapshot = await _comparisonService.CreateSnapshotAsync(
                vesselId,
                userId,
                dto,
                displayUnits,
                cancellationToken);

            // Return the created snapshot as DTO
            var snapshotDto = await _comparisonService.GetSnapshotByIdAsync(
                snapshot.Id,
                displayUnits,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetSnapshot),
                new { snapshotId = snapshot.Id },
                snapshotDto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comparison snapshot for vessel {VesselId}", vesselId);
            return StatusCode(500, new { error = "Failed to create snapshot" });
        }
    }

    /// <summary>
    /// Get all comparison snapshots for a vessel
    /// </summary>
    [HttpGet("vessels/{vesselId}/snapshots")]
    public async Task<ActionResult<List<ComparisonSnapshotDto>>> GetSnapshots(
        Guid vesselId,
        [FromQuery] string displayUnits = "SI",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshots = await _comparisonService.GetSnapshotsAsync(
                vesselId,
                displayUnits,
                cancellationToken);

            return Ok(snapshots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting snapshots for vessel {VesselId}", vesselId);
            return StatusCode(500, new { error = "Failed to retrieve snapshots" });
        }
    }

    /// <summary>
    /// Get a single comparison snapshot by ID
    /// </summary>
    [HttpGet("snapshots/{snapshotId}")]
    public async Task<ActionResult<ComparisonSnapshotDto>> GetSnapshot(
        Guid snapshotId,
        [FromQuery] string displayUnits = "SI",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await _comparisonService.GetSnapshotByIdAsync(
                snapshotId,
                displayUnits,
                cancellationToken);

            if (snapshot == null)
            {
                return NotFound(new { error = $"Snapshot {snapshotId} not found" });
            }

            return Ok(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting snapshot {SnapshotId}", snapshotId);
            return StatusCode(500, new { error = "Failed to retrieve snapshot" });
        }
    }

    /// <summary>
    /// Update a comparison snapshot (e.g., set as baseline)
    /// </summary>
    [HttpPatch("snapshots/{snapshotId}")]
    public async Task<ActionResult<ComparisonSnapshotDto>> UpdateSnapshot(
        Guid snapshotId,
        [FromBody] UpdateComparisonSnapshotDto dto,
        [FromQuery] string displayUnits = "SI",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await _comparisonService.UpdateSnapshotAsync(
                snapshotId,
                dto,
                cancellationToken);

            if (updated == null)
            {
                return NotFound(new { error = $"Snapshot {snapshotId} not found" });
            }

            var snapshotDto = await _comparisonService.GetSnapshotByIdAsync(
                snapshotId,
                displayUnits,
                cancellationToken);

            return Ok(snapshotDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating snapshot {SnapshotId}", snapshotId);
            return StatusCode(500, new { error = "Failed to update snapshot" });
        }
    }

    /// <summary>
    /// Delete a comparison snapshot (soft delete)
    /// </summary>
    [HttpDelete("snapshots/{snapshotId}")]
    public async Task<ActionResult> DeleteSnapshot(
        Guid snapshotId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _comparisonService.DeleteSnapshotAsync(snapshotId, cancellationToken);

            if (!deleted)
            {
                return NotFound(new { error = $"Snapshot {snapshotId} not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting snapshot {SnapshotId}", snapshotId);
            return StatusCode(500, new { error = "Failed to delete snapshot" });
        }
    }

    /// <summary>
    /// Compare two snapshots (baseline vs candidate)
    /// </summary>
    [HttpPost("compare")]
    public async Task<ActionResult<ComparisonReportDto>> CompareSnapshots(
        [FromBody] CompareRunsRequestDto dto,
        [FromQuery] string displayUnits = "SI",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _comparisonService.CompareSnapshotsAsync(
                dto.BaselineSnapshotId,
                dto.CandidateSnapshotId,
                displayUnits,
                cancellationToken);

            return Ok(report);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing snapshots");
            return StatusCode(500, new { error = "Failed to compare snapshots" });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        return userId;
    }
}

