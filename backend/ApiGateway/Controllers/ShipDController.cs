using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/shipd")]
public class ShipDController : ControllerBase
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<ShipDController> _logger;

    public ShipDController(IHttpClientService httpClientService, ILogger<ShipDController> logger)
    {
        _httpClientService = httpClientService;
        _logger = logger;
    }

    [HttpGet]
    [HttpGet("{**path}")]
    public async Task<IActionResult> GetProxy(string? path, CancellationToken cancellationToken)
    {
        var targetPath = string.IsNullOrWhiteSpace(path) ? string.Empty : path;
        try
        {
            _logger.LogInformation("Proxying GET /shipd/{Path} to DataService", targetPath);
            var response = await _httpClientService.GetAsync("data", $"api/v1/shipd/{targetPath}", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying ShipD GET request to DataService for path {Path}", targetPath);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
}
