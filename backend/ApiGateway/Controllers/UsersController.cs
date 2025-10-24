using Microsoft.AspNetCore.Mvc;
using ApiGateway.Services;
using Shared.DTOs;
using Shared.Validators;
using System.Text;
using System.Text.Json;

namespace ApiGateway.Controllers;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<UsersController> _logger;
    private readonly CreateUserDtoValidator _createUserValidator;

    public UsersController(
        IHttpClientService httpClientService,
        ILogger<UsersController> logger,
        CreateUserDtoValidator createUserValidator)
    {
        _httpClientService = httpClientService;
        _logger = logger;
        _createUserValidator = createUserValidator;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id, CancellationToken cancellationToken)
    {
        var response = await _httpClientService.GetAsync("identity", $"api/v1/users/{id}", cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return StatusCode((int)response.StatusCode, content);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserDto dto,
        CancellationToken cancellationToken)
    {
        // Validate input
        var validationResult = await _createUserValidator.ValidateAsync(dto, cancellationToken);
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

        var response = await _httpClientService.PostAsync("identity", "api/v1/users", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return StatusCode((int)response.StatusCode, responseContent);
    }
}





