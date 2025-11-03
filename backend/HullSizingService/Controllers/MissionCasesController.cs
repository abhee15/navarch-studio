using FluentValidation;
using HullSizingService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Sizing;

namespace HullSizingService.Controllers;

/// <summary>
/// API controller for managing mission cases
/// </summary>
[ApiController]
[Route("api/v1/hull-sizing/mission-cases")]
public class MissionCasesController : ControllerBase
{
    private readonly IMissionCaseService _missionCaseService;
    private readonly IValidator<CreateMissionCaseDto> _createValidator;
    private readonly IValidator<UpdateMissionCaseDto> _updateValidator;
    private readonly ILogger<MissionCasesController> _logger;

    public MissionCasesController(
        IMissionCaseService missionCaseService,
        IValidator<CreateMissionCaseDto> createValidator,
        IValidator<UpdateMissionCaseDto> updateValidator,
        ILogger<MissionCasesController> logger)
    {
        _missionCaseService = missionCaseService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    /// <summary>
    /// Get all mission cases for the current tenant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<MissionCaseDto>>> GetAll(CancellationToken cancellationToken)
    {
        // TODO: Extract tenantId from claims (Phase 1: use hardcoded dev tenant)
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-tenant";

        var missionCases = await _missionCaseService.GetAllAsync(tenantId, cancellationToken);
        return Ok(missionCases);
    }

    /// <summary>
    /// Get a specific mission case by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MissionCaseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-tenant";

        var missionCase = await _missionCaseService.GetByIdAsync(id, tenantId, cancellationToken);
        if (missionCase == null)
        {
            return NotFound(new { error = "Mission case not found or access denied" });
        }

        return Ok(missionCase);
    }

    /// <summary>
    /// Create a new mission case
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MissionCaseDto>> Create(
        [FromBody] CreateMissionCaseDto dto,
        CancellationToken cancellationToken)
    {
        // Validate
        var validationResult = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                error = "Validation failed",
                errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });
        }

        // TODO: Extract userId and tenantId from claims (Phase 1: use hardcoded)
        var userId = HttpContext.Items["Claims:Sub"] != null
            ? Guid.Parse(HttpContext.Items["Claims:Sub"]!.ToString()!)
            : Guid.NewGuid();
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-tenant";

        var missionCase = await _missionCaseService.CreateAsync(dto, userId, tenantId, cancellationToken);

        _logger.LogInformation("[MISSION_CONTROLLER] Mission case created: {Id}", missionCase.Id);

        return CreatedAtAction(nameof(GetById), new { id = missionCase.Id }, missionCase);
    }

    /// <summary>
    /// Update an existing mission case
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<MissionCaseDto>> Update(
        Guid id,
        [FromBody] UpdateMissionCaseDto dto,
        CancellationToken cancellationToken)
    {
        // Validate
        var validationResult = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                error = "Validation failed",
                errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });
        }

        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-tenant";

        var missionCase = await _missionCaseService.UpdateAsync(id, dto, tenantId, cancellationToken);
        if (missionCase == null)
        {
            return NotFound(new { error = "Mission case not found or access denied" });
        }

        _logger.LogInformation("[MISSION_CONTROLLER] Mission case updated: {Id}", id);

        return Ok(missionCase);
    }

    /// <summary>
    /// Soft delete a mission case
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-tenant";

        var success = await _missionCaseService.DeleteAsync(id, tenantId, cancellationToken);
        if (!success)
        {
            return NotFound(new { error = "Mission case not found or access denied" });
        }

        _logger.LogInformation("[MISSION_CONTROLLER] Mission case deleted: {Id}", id);

        return NoContent();
    }
}

