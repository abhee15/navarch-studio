using Microsoft.AspNetCore.Mvc;
using ApiGateway.Services;

namespace ApiGateway.Controllers;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IHttpClientService httpClientService, ILogger<ProductsController> logger)
    {
        _httpClientService = httpClientService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        var response = await _httpClientService.GetAsync("data", "api/v1/products", cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return StatusCode((int)response.StatusCode, content);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(string id, CancellationToken cancellationToken)
    {
        var response = await _httpClientService.GetAsync("data", $"api/v1/products/{id}", cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return StatusCode((int)response.StatusCode, content);
    }
}





