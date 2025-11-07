using FluentValidation;
using HullSizingService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Sizing;

namespace HullSizingService.Controllers;

[ApiController]
[Route("api/v1/hull-sizing/mission-cases")]
public class MissionCasesController : ControllerBase
{
    private readonly IMissionCaseService _service;
    private readonly IValidator<CreateMissionCaseDto> _createValidator;
    private readonly IValidator<UpdateMissionCaseDto> _updateValidator;
    private readonly ILogger<MissionCasesController> _logger;

    public MissionCasesController(
        IMissionCaseService service,
        IValidator<CreateMissionCaseDto> createValidator,
        IValidator<UpdateMissionCaseDto> updateValidator,
        ILogger<MissionCasesController> logger)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<MissionCaseDto>>> GetAll(CancellationToken ct)
    {
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-default-tenant";
        var cases = await _service.GetAllAsync(tenantId, ct);
        return Ok(cases);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MissionCaseDto>> GetById(Guid id, CancellationToken ct)
    {
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-default-tenant";
        var missionCase = await _service.GetByIdAsync(id, tenantId, ct);

        if (missionCase == null)
            return NotFound(new { error = "Mission case not found" });

        return Ok(missionCase);
    }

    [HttpPost]
    public async Task<ActionResult<MissionCaseDto>> Create([FromBody] CreateMissionCaseDto dto, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                error = "Validation failed",
                errors = validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });
        }

        // Try to parse userId as GUID, fallback to generated GUID if not valid (dev mode)
        var userIdStr = HttpContext.Items["Claims:Sub"]?.ToString();
        var userId = Guid.TryParse(userIdStr, out var parsedUserId) ? parsedUserId : Guid.NewGuid();
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-default-tenant";

        var result = await _service.CreateAsync(dto, userId, tenantId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MissionCaseDto>> Update(Guid id, [FromBody] UpdateMissionCaseDto dto, CancellationToken ct)
    {
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                error = "Validation failed",
                errors = validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });
        }

        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-default-tenant";
        var result = await _service.UpdateAsync(id, dto, tenantId, ct);

        if (result == null)
            return NotFound(new { error = "Mission case not found" });

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-default-tenant";
        var success = await _service.DeleteAsync(id, tenantId, ct);

        if (!success)
            return NotFound(new { error = "Mission case not found" });

        return NoContent();
    }
}
