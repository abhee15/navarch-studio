using FluentValidation;
using HullSizingService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Shared.DTOs.Sizing;

namespace HullSizingService.Controllers;

[ApiController]
[Route("api/v1/hull-sizing/runs")]
public class SizingRunsController : ControllerBase
{
    private readonly ISizingRunService _service;
    private readonly IValidator<CreateSizingRunDto> _validator;
    private readonly ILogger<SizingRunsController> _logger;
    private readonly IHostEnvironment _environment;

    public SizingRunsController(
        ISizingRunService service,
        IValidator<CreateSizingRunDto> validator,
        ILogger<SizingRunsController> logger,
        IHostEnvironment environment)
    {
        _service = service;
        _validator = validator;
        _logger = logger;
        _environment = environment;
    }

    [HttpGet("mission-case/{missionCaseId}")]
    public async Task<ActionResult<List<SizingRunDto>>> GetByMissionCase(Guid missionCaseId, CancellationToken ct)
    {
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-default-tenant";
        var runs = await _service.GetByMissionCaseIdAsync(missionCaseId, tenantId, ct);
        return Ok(runs);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SizingRunDto>> GetById(Guid id, CancellationToken ct)
    {
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-default-tenant";
        var run = await _service.GetByIdAsync(id, tenantId, ct);

        if (run == null)
            return NotFound(new { error = "Sizing run not found" });

        return Ok(run);
    }

    [HttpPost]
    public async Task<ActionResult<SizingRunDto>> Create([FromBody] CreateSizingRunDto dto, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(dto, ct);
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

        try
        {
            var result = await _service.CreateAsync(dto, userId, tenantId, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating sizing run");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while creating sizing run. Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                ex.GetType().Name, ex.Message, ex.StackTrace);

            // Include exception details in staging/dev for debugging
            var errorResponse = new
            {
                error = "An unexpected error occurred while processing your request",
                message = _environment.IsDevelopment() || _environment.IsStaging()
                    ? ex.Message
                    : "An unexpected error occurred while processing your request",
                type = _environment.IsDevelopment() || _environment.IsStaging()
                    ? ex.GetType().Name
                    : null,
                stackTrace = _environment.IsDevelopment()
                    ? ex.StackTrace
                    : null
            };

            return StatusCode(500, errorResponse);
        }
    }

    [HttpGet("{id}/candidates")]
    public async Task<ActionResult<List<CandidateDesignDto>>> GetCandidates(Guid id, CancellationToken ct)
    {
        var tenantId = HttpContext.Items["Claims:TenantId"]?.ToString() ?? "dev-default-tenant";
        var candidates = await _service.GetCandidatesAsync(id, tenantId, ct);
        return Ok(candidates);
    }
}
