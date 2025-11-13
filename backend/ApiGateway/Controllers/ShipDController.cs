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

    [HttpGet("{**path}")]
    public async Task<IActionResult> GetProxy(string path, CancellationToken cancellationToken)
    {
        var targetPath = path ?? string.Empty;
        try
        {
            _logger.LogInformation("Proxying GET /shipd/{Path} to DataService", targetPath);
            var response = await _httpClientService.GetAsync("data", $"api/v1/shipd/{targetPath}", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // Return raw content to avoid double JSON encoding
            // Use Content() to preserve the content type from the downstream service
            return Content(content, response.Content.Headers.ContentType?.ToString() ?? "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying ShipD GET request to DataService for path {Path}", targetPath);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
}
