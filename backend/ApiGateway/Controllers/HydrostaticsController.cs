using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

/// <summary>
/// Controller that proxies hydrostatics requests to the DataService
/// </summary>
[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hydrostatics")]
public class HydrostaticsController : ControllerBase
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<HydrostaticsController> _logger;

    public HydrostaticsController(
        IHttpClientService httpClientService,
        ILogger<HydrostaticsController> logger)
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
            _logger.LogInformation("Proxying GET /hydrostatics/{Path} to DataService", path);
            var response = await _httpClientService.GetAsync("data", $"api/v1/hydrostatics/{path}", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // Return raw content to avoid double JSON encoding
            return Content(content, response.Content.Headers.ContentType?.ToString() ?? "application/json");
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
            _logger.LogInformation("Proxying POST /hydrostatics/{Path} to DataService", path);

            var contentType = Request.ContentType;

            // For multipart/form-data (file uploads), parse and reconstruct the form
            if (!string.IsNullOrEmpty(contentType) && contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                // Read the form from the request
                var form = await Request.ReadFormAsync(cancellationToken);

                // Create MultipartFormDataContent to properly reconstruct the form
                var multipartContent = new MultipartFormDataContent();

                // Add all form fields
                foreach (var field in form)
                {
                    multipartContent.Add(new StringContent(field.Value), field.Key);
                }

                // Add all files
                foreach (var file in form.Files)
                {
                    var fileContent = new StreamContent(file.OpenReadStream());
                    fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(file.ContentType ?? "application/octet-stream");
                    multipartContent.Add(fileContent, file.Name, file.FileName);
                }

                var response = await _httpClientService.PostAsync("data", $"api/v1/hydrostatics/{path}", multipartContent, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                // Return raw content to avoid double JSON encoding
                return Content(responseContent, response.Content.Headers.ContentType?.ToString() ?? "application/json");
            }
            else
            {
                // For JSON/text, read as string
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync(cancellationToken);
                var content = new StringContent(body, System.Text.Encoding.UTF8, contentType ?? "application/json");

                var response = await _httpClientService.PostAsync("data", $"api/v1/hydrostatics/{path}", content, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                // Return raw content to avoid double JSON encoding
                return Content(responseContent, response.Content.Headers.ContentType?.ToString() ?? "application/json");
            }
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
            _logger.LogInformation("Proxying PUT /hydrostatics/{Path} to DataService", path);

            // Read the request body
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync(cancellationToken);
            var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClientService.PutAsync("data", $"api/v1/hydrostatics/{path}", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            // Return raw content to avoid double JSON encoding
            return Content(responseContent, response.Content.Headers.ContentType?.ToString() ?? "application/json");
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
            _logger.LogInformation("Proxying DELETE /hydrostatics/{Path} to DataService", path);
            var response = await _httpClientService.DeleteAsync("data", $"api/v1/hydrostatics/{path}", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // Return raw content to avoid double JSON encoding
            return Content(content, response.Content.Headers.ContentType?.ToString() ?? "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying DELETE request to DataService");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
}
