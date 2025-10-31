using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

/// <summary>
/// Controller that proxies resistance and powering requests to the DataService
/// </summary>
[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/resistance")]
public class ResistanceController : ControllerBase
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<ResistanceController> _logger;

    public ResistanceController(
        IHttpClientService httpClientService,
        ILogger<ResistanceController> logger)
    {
        _httpClientService = httpClientService;
        _logger = logger;
    }

    /// <summary>
    /// Proxy GET requests to DataService
    /// </summary>
    [HttpGet("{**path}")]
    public async Task<IActionResult> GetProxy(string path, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Proxying GET /resistance/{Path} to DataService", path);
            var response = await _httpClientService.GetAsync("data", $"api/v1/resistance/{path}", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying GET request to DataService");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Proxy POST requests to DataService
    /// </summary>
    [HttpPost("{**path}")]
    public async Task<IActionResult> PostProxy(string path, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Proxying POST /resistance/{Path} to DataService", path);
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            var content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClientService.PostAsync(
                "data",
                $"api/v1/resistance/{path}",
                content,
                cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            return StatusCode((int)response.StatusCode, responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying POST request to DataService");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Proxy PUT requests to DataService
    /// </summary>
    [HttpPut("{**path}")]
    public async Task<IActionResult> PutProxy(string path, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Proxying PUT /resistance/{Path} to DataService", path);
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            var content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClientService.PutAsync(
                "data",
                $"api/v1/resistance/{path}",
                content,
                cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            return StatusCode((int)response.StatusCode, responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying PUT request to DataService");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Proxy DELETE requests to DataService
    /// </summary>
    [HttpDelete("{**path}")]
    public async Task<IActionResult> DeleteProxy(string path, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Proxying DELETE /resistance/{Path} to DataService", path);
            var response = await _httpClientService.DeleteAsync("data", $"api/v1/resistance/{path}", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying DELETE request to DataService");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
}

