using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

/// <summary>
/// Controller that proxies AI agent requests to the AIAgentService
/// </summary>
[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai-agent")]
public class AIAgentController : ControllerBase
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<AIAgentController> _logger;

    public AIAgentController(
        IHttpClientService httpClientService,
        ILogger<AIAgentController> logger)
    {
        _httpClientService = httpClientService;
        _logger = logger;
    }

    /// <summary>
    /// Proxy GET requests to AIAgentService
    /// </summary>
    [HttpGet("{**path}")]
    public async Task<IActionResult> GetProxy(string path, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Proxying GET /ai-agent/{Path} to AIAgentService", path);
            var response = await _httpClientService.GetAsync("aiagent", $"api/v1/ai-agent/{path}", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ContentResult
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying GET request to AIAgentService");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Proxy POST requests to AIAgentService
    /// </summary>
    [HttpPost("{**path}")]
    public async Task<IActionResult> PostProxy(string path, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Proxying POST /ai-agent/{Path} to AIAgentService", path);
            var content = new StreamContent(Request.Body);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(Request.ContentType ?? "application/json");
            var response = await _httpClientService.PostAsync("aiagent", $"api/v1/ai-agent/{path}", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ContentResult
            {
                Content = responseContent,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying POST request to AIAgentService");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Proxy PUT requests to AIAgentService
    /// </summary>
    [HttpPut("{**path}")]
    public async Task<IActionResult> PutProxy(string path, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Proxying PUT /ai-agent/{Path} to AIAgentService", path);
            var content = new StreamContent(Request.Body);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(Request.ContentType ?? "application/json");
            var response = await _httpClientService.PutAsync("aiagent", $"api/v1/ai-agent/{path}", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ContentResult
            {
                Content = responseContent,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying PUT request to AIAgentService");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Proxy DELETE requests to AIAgentService
    /// </summary>
    [HttpDelete("{**path}")]
    public async Task<IActionResult> DeleteProxy(string path, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Proxying DELETE /ai-agent/{Path} to AIAgentService", path);
            var response = await _httpClientService.DeleteAsync("aiagent", $"api/v1/ai-agent/{path}", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ContentResult
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying DELETE request to AIAgentService");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
}
