using System.Text;
using System.Text.Json;
using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shared.DTOs;
using Shared.Validators;

namespace ApiGateway.Controllers;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<AuthController> _logger;
    private readonly LoginDtoValidator _loginValidator;

    public AuthController(
        IHttpClientService httpClientService,
        ILogger<AuthController> logger,
        LoginDtoValidator loginValidator)
    {
        _httpClientService = httpClientService;
        _logger = logger;
        _loginValidator = loginValidator;
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]  // 5 attempts per 15 minutes
    public async Task<IActionResult> Login(
        [FromBody] LoginDto dto,
        CancellationToken cancellationToken)
    {
        // Validate input
        var validationResult = await _loginValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                errors = validationResult.Errors.Select(e => new
                {
                    property = e.PropertyName,
                    message = e.ErrorMessage
                })
            });
        }

        var json = JsonSerializer.Serialize(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClientService.PostAsync("identity", "api/v1/auth/login", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return StatusCode((int)response.StatusCode, responseContent);
    }
}





