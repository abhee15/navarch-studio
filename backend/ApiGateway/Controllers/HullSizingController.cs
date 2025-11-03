using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

/// <summary>
/// Controller that proxies hull sizing requests to the HullSizingService
/// </summary>
[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hull-sizing")]
public class HullSizingController : ControllerBase
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<HullSizingController> _logger;

    public HullSizingController(
        IHttpClientService httpClientService,
        ILogger<HullSizingController> logger)
    {
        _httpClientService = httpClientService;
        _logger = logger;
    }

    /// <summary>
    /// Proxy GET requests to HullSizingService
    /// </summary>
    [HttpGet("{**path}")]
    public async Task<IActionResult> GetProxy(string path, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Proxying GET /hull-sizing/{Path} to HullSizingService", path);
            var response = await _httpClientService.GetAsync("hullsizing", $"api/v1/hull-sizing/{path}", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // Return raw content with actual status code from downstream service
            return new ContentResult
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying GET request to HullSizingService");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Proxy POST requests to HullSizingService
    /// </summary>
    [HttpPost("{**path}")]
    public async Task<IActionResult> PostProxy(string path, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Proxying POST /hull-sizing/{Path} to HullSizingService", path);

            // Read request body
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync(cancellationToken);
            var content = new StringContent(body, System.Text.Encoding.UTF8, Request.ContentType ?? "application/json");

            var response = await _httpClientService.PostAsync(
                "hullsizing",
                $"api/v1/hull-sizing/{path}",
                content,
                cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            // Return raw content with actual status code from downstream service
            return new ContentResult
            {
                Content = responseContent,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying POST request to HullSizingService");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Proxy PUT requests to HullSizingService
    /// </summary>
    [HttpPut("{**path}")]
    public async Task<IActionResult> PutProxy(string path, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Proxying PUT /hull-sizing/{Path} to HullSizingService", path);

            // Read request body
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync(cancellationToken);
            var content = new StringContent(body, System.Text.Encoding.UTF8, Request.ContentType ?? "application/json");

            var response = await _httpClientService.PutAsync(
                "hullsizing",
                $"api/v1/hull-sizing/{path}",
                content,
                cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            // Return raw content with actual status code from downstream service
            return new ContentResult
            {
                Content = responseContent,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying PUT request to HullSizingService");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Proxy DELETE requests to HullSizingService
    /// </summary>
    [HttpDelete("{**path}")]
    public async Task<IActionResult> DeleteProxy(string path, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Proxying DELETE /hull-sizing/{Path} to HullSizingService", path);
            var response = await _httpClientService.DeleteAsync("hullsizing", $"api/v1/hull-sizing/{path}", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // Return raw content to avoid double JSON encoding
            return Content(content, response.Content.Headers.ContentType?.ToString() ?? "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying DELETE request to HullSizingService");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
}

