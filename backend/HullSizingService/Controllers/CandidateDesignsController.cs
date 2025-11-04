using HullSizingService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Sizing;

namespace HullSizingService.Controllers;

[ApiController]
[Route("api/v1/hull-sizing/candidates")]
public class CandidateDesignsController : ControllerBase
{
    private readonly ICandidateDesignService _service;
    private readonly ILogger<CandidateDesignsController> _logger;

    public CandidateDesignsController(
        ICandidateDesignService service,
        ILogger<CandidateDesignsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CandidateDesignDto>> GetById(Guid id, CancellationToken ct)
    {
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-tenant";
        var candidate = await _service.GetByIdAsync(id, tenantId, ct);

        if (candidate == null)
            return NotFound(new { error = "Candidate design not found" });

        return Ok(candidate);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CandidateDesignDto>> Update(Guid id, [FromBody] UpdateCandidateDesignDto dto, CancellationToken ct)
    {
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-tenant";
        var result = await _service.UpdateAsync(id, dto, tenantId, ct);

        if (result == null)
            return NotFound(new { error = "Candidate design not found" });

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-tenant";
        var success = await _service.DeleteAsync(id, tenantId, ct);

        if (!success)
            return NotFound(new { error = "Candidate design not found" });

        return NoContent();
    }

    [HttpPost("{id}/export/json")]
    public async Task<ActionResult> ExportJson(Guid id, CancellationToken ct)
    {
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-tenant";
        var json = await _service.ExportJsonAsync(id, tenantId, ct);

        if (json == null)
            return NotFound(new { error = "Candidate design not found" });

        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"candidate_{id}.json");
    }

    [HttpPost("{id}/export/csv")]
    public async Task<ActionResult> ExportCsv(Guid id, CancellationToken ct)
    {
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-tenant";
        var csv = await _service.ExportCsvAsync(id, tenantId, ct);

        if (csv == null)
            return NotFound(new { error = "Candidate design not found" });

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"candidate_{id}.csv");
    }

    [HttpPost("{id}/adjust")]
    public async Task<ActionResult<CandidateDesignDto>> AdjustParameter(
        Guid id,
        [FromBody] AdjustParameterDto dto,
        CancellationToken ct)
    {
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-tenant";

        try
        {
            var result = await _service.AdjustParameterAsync(id, dto, tenantId, ct);

            if (result == null)
                return NotFound(new { error = "Candidate design not found" });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}


